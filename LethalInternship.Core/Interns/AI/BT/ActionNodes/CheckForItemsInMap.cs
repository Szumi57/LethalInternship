using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForItemsInMap : IBTAction
    {
        private List<int> indices = new List<int>();
        private List<GrabbableObject> itemsToCheck = new List<GrabbableObject>();

        private int itemIndex = 0;
        private int randomIndex = 0;

        private List<PathController> tempPaths = new List<PathController>();
        private List<PathfindingContext> tempPfs = new List<PathfindingContext>();
        private List<int> pathIds = new List<int>();

        private readonly List<IInstruction> instructionsToProcess = new List<IInstruction>(1024);

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.TargetItem != null)
            {
                if (InternManager.Instance.IsGrabbableObjectGrabbable(context.TargetItem))
                    return BehaviourTreeStatus.Success;

                Debug.Log($"--------- {ai.Npc.playerUsername} CheckForItemsInMap context.TargetItem {context.TargetItem.name} not grabbable !!!!");
                CleanItemsToCheck(context);
            }

            if (itemsToCheck.Count == 0)
            {
                itemIndex = 0;
                CleanTempLists();

                InternManager.Instance.LookingForItemsToGrabInMap(itemsToCheck);
                for (int i = 0; i < itemsToCheck.Count; i++)
                {
                    //Debug.Log($"{ai.Npc.playerUsername} itemsToCheck[{i}] ? {itemsToCheck[i].itemProperties.itemName}");
                }

                if (itemsToCheck.Count == 0)
                {
                    context.TargetItem = null;
                    if (!context.cancelScavenging) { ai.TryPlayCantDoCommandVoiceAudio(); }
                    context.cancelScavenging = true;
                    CleanItemsToCheck(context);
                    return BehaviourTreeStatus.Success;
                }
            }

            if (context.NbItemsToCheck != itemsToCheck.Count)
            {
                context.UpdateNbItemsToCheck(itemsToCheck.Count); // Count nb items to check

                // Use random indexes
                indices = Enumerable.Range(0, itemsToCheck.Count).ToList();

                // Randomize indexes
                Random rng = new Random();
                for (int i = itemsToCheck.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }
            }

            for (int i = 0; i < itemsToCheck.Count; i++)
            {
                if (i >= tempPfs.Count) continue;
                var pf = tempPfs[i];
                if (pf != null && pf.Destination != null)
                {
                    if (!pf.Destination.ToString().Replace(" ", "").ToLowerInvariant().Contains(itemsToCheck[i].itemProperties.itemName.Replace(" ", "").ToLowerInvariant()))
                    {
                        Debug.Log($"{ai.Npc.playerUsername} itemsToCheck[{i}] {itemsToCheck[i].itemProperties.itemName} != pf {pf.Destination.ToString()}");
                        for (int j = 0; j < tempPfs.Count; j++)
                        {
                            if (tempPfs[j] != null)
                            {
                                Debug.Log($"?? temppf ? {ai.Npc.playerUsername} pf i={j} {tempPfs[j].Destination.ToString()}");
                            }
                        }
                    }
                }
            }

            // Check for at least one good path to item
            if (itemsToCheck.Count > 0)
            {
                int indexItemToGrab = GetIndexMinPath();
                if (indexItemToGrab >= 0)
                {
                    // ++ Path to one item found
                    if (indexItemToGrab >= itemsToCheck.Count)
                    {
                        Debug.Log($"!!! indexItemToGrab {indexItemToGrab} itemsToCheck.Count {itemsToCheck.Count}");
                        foreach (var a in itemsToCheck)
                        {
                            Debug.Log($"!!! itemName {a.itemProperties.itemName} {a.transform.position}");
                        }
                        Debug.Log($"!!! tempPfs {tempPfs.Count} tempPaths {tempPaths.Count} {tempPfs[indexItemToGrab].Destination}");
                        Debug.Log($"!!! {tempPfs[indexItemToGrab].GetFullPathString(tempPaths[indexItemToGrab].PathIds)}");
                    }
                    context.TargetItem = itemsToCheck[indexItemToGrab];
                    context.PathfindingContext.CopyFrom(tempPfs[indexItemToGrab]);
                    context.PathController.CopyFrom(tempPaths[indexItemToGrab]);
                    PluginLoggerHook.LogDebug?.Invoke($"++M {ai.Npc.playerUsername} CheckForItemsInMap target item {context.TargetItem} {context.TargetItem.transform.position}, valid {context.PathController.IsPathValid()} {context.PathfindingContext.GetFullPathString(context.PathController.PathIds)} {context.PathfindingContext.Destination}");

                    TryPlayNowScavengingVoiceAudio(ai);
                    CleanItemsToCheck(context);
                    return BehaviourTreeStatus.Success;
                }
            }

            // We checked everything
            if (itemIndex >= itemsToCheck.Count)
            {
                PluginLoggerHook.LogDebug?.Invoke($"??M {ai.Npc.playerUsername} NOTHING more grabbable on map");
                context.cancelScavenging = true;
            }

            if (context.cancelScavenging)
            {
                CleanItemsToCheck(context);

                if (ai.AreHandsFree())
                {
                    ai.TryPlayCantDoCommandVoiceAudio();
                    ai.SetCommandToFollowPlayer(playVoice: false);
                    return BehaviourTreeStatus.Success;
                }

                // return scavenged items to ship
                context.TargetItem = null;
                return BehaviourTreeStatus.Failure;
            }

            randomIndex = indices[itemIndex];
            //PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} CheckForItemsInMap begin CalculatePathToItem random index = {randomIndex}, itemIndex = {itemIndex} itemsToCheck.Count {itemsToCheck.Count}");
            CalculatePathToItem(context, itemsToCheck[randomIndex]);

            return BehaviourTreeStatus.Success;
        }

        private void CleanItemsToCheck(BTContext context)
        {
            itemIndex = 0;
            //Debug.Log($"vvvvvvvvvvvvvvv {context.InternAI.Npc.playerUsername} itemsToCheck.Clear() vvvvvvvvvvvvv \r\n {Environment.StackTrace}");
            itemsToCheck.Clear();
            context.UpdateNbItemsToCheck(0);
        }

        private void CalculatePathToItem(BTContext context, GrabbableObject grabbableObject)
        {
            InternAI ai = context.InternAI;

            PathfindingContext pf = GetNewPathfindingContext(randomIndex);
            pf.Clear();
            pf.SharedGraph = InternManager.Instance.GetGraphEntrances();

            // Add start
            DJKStaticPoint dJKPointStart = InternManager.Instance.Pools.Get<DJKStaticPoint>();
            dJKPointStart.Position = Dijkstra.Dijkstra.GetSampledPos(ai.transform.position);
            dJKPointStart.Name = $"{ai.Npc.playerUsername} pos";
            pf.SetStart(dJKPointStart);
            // Destination
            DJKItemPoint dJKPointDest = InternManager.Instance.Pools.Get<DJKItemPoint>();
            dJKPointDest.Transform = grabbableObject.transform;
            dJKPointDest.GrabDistance = ai.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale;
            dJKPointDest.SetName(grabbableObject);
            pf.SetDestination(dJKPointDest);

            NeighborResult startWriter = (from, to, startPos, targetPos, dist) =>
            {
                Debug.Log($"{ai.Npc.playerUsername} CheckForItemsInMap adding neighbors to start : from {to} to {from} startPos {startPos} targetPos {targetPos} dist {dist}");
                pf.StartNeighbors.Add(new DJKNeighbor(to, targetPos, dist));
            };
            NeighborResult destinationWriter = (from, to, startPos, targetPos, dist) =>
            {
                Debug.Log($"{ai.Npc.playerUsername} CheckForItemsInMap adding neighbors to dest : from {to} to {from} startPos {startPos} targetPos {targetPos} dist {dist}");
                pf.DestinationNeighbors.Add(new DJKNeighbor(from, targetPos, dist));
            };

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            Dijkstra.Dijkstra.GenerateNeighborInstructions(pf, idBatch, startWriter, destinationWriter, instructionsToProcess);
            PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} CheckForItemsInMap RequestBatch idBatch={idBatch} dest {dJKPointDest} itemIndex={itemIndex} randomIndex={randomIndex}");
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
        }

        private void OnBatchCompleted()
        {
            // Get full path
            PathController pathCalculated = GetNewPathController(randomIndex);
            pathCalculated.Reset();

            PathfindingContext pf = tempPfs[randomIndex];
            //PluginLoggerHook.LogDebug?.Invoke($"CheckForItemsToGrabInMap itemIndex {itemIndex} , random i {randomIndex} pf.Start {pf.Start.Id} pf.Destination {pf.Destination.Id} {tempPfs[randomIndex].SharedGraph}");
            Dijkstra.Dijkstra.CalculatePath(pf,
                                            pf.Start.Id,
                                            pf.Destination.Id,
                                            pathIds);
            pathCalculated.SetNewPath(pathIds);

            // log
            PluginLoggerHook.LogDebug?.Invoke($"=> CheckForItemsToGrabInMap itemIndex {itemIndex} => {itemIndex + 1}, random i {randomIndex} valid {pathCalculated.IsPathValid()} ======= {tempPfs[randomIndex].GetFullPathString(pathCalculated.PathIds)} {tempPfs[randomIndex].Destination}");

            itemIndex++;
        }

        private int GetIndexMinPath()
        {
            int indexBestPath = -1;
            float minDist = float.MaxValue;
            for (int i = 0; i < itemsToCheck.Count; i++)
            {
                if (i >= tempPaths.Count)
                {
                    break;
                }

                PathController tempPath = tempPaths[i];
                if (tempPath == null || !tempPath.IsPathValid())
                {
                    continue;
                }

                float dist = tempPfs[i].GetFullPathDistance(tempPath.PathIds);
                if (dist < minDist)
                {
                    minDist = dist;
                    indexBestPath = i;
                }
            }
            return indexBestPath;
        }

        private PathfindingContext GetNewPathfindingContext(int index)
        {
            // Resize until ok
            while (tempPfs.Count <= index)
                tempPfs.Add(null!);

            PathfindingContext pf = tempPfs[index];
            if (pf == null)
            {
                pf = new PathfindingContext();
                tempPfs[index] = pf;
            }
            return pf;
        }

        private PathController GetNewPathController(int index)
        {
            // Resize until ok
            while (tempPaths.Count <= index)
                tempPaths.Add(null!);

            PathController pc = tempPaths[index];
            if (pc == null)
            {
                pc = new PathController();
                tempPaths[index] = pc;
            }
            return pc;
        }

        private void CleanTempLists()
        {
            foreach (var path in tempPaths)
            {
                if (path != null)
                    path.Reset();
            }

            foreach (var pf in tempPfs)
            {
                if (pf != null)
                    pf.Clear();
            }
        }

        private void TryPlayNowScavengingVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.NowScavenging,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
