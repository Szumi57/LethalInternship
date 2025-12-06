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
        private int count => itemsToCheck.Count;

        private int itemIndex = 0;
        private int randomIndex = 0;

        private GraphController[] tempGraphs = null!;
        private PathController[] tempPaths = null!;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.TargetItem != null
                && ai.IsGrabbableObjectGrabbable(context.TargetItem))
            {
                return BehaviourTreeStatus.Success;
            }

            if (itemsToCheck.Count == 0)
            {
                itemIndex = 0;
                itemsToCheck = LookingForItemsToGrabInMap(ai);
                context.nbItemsToCheck = count; // Count nb items to check
                if (itemsToCheck.Count == 0)
                {
                    context.TargetItem = null;
                    ai.TryPlayCantDoCommandVoiceAudio();
                    // todo : stay in place ?
                    if (!ai.AreHandsFree())
                    {
                        ai.SetCommandToFollowPlayer(playVoice: false);
                    }
                    return BehaviourTreeStatus.Failure;
                }

                tempGraphs = new GraphController[itemsToCheck.Count];
                tempPaths = new PathController[itemsToCheck.Count];

                // Use random indexes
                indices = Enumerable.Range(0, count).ToList();

                // Randomize indexes
                Random rng = new Random();
                for (int i = count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }
            }

            // Check for at least one good path to item
            if (count > 0)
            {
                int indexItemToGrab = GetIndexMinPath();
                if (indexItemToGrab >= 0)
                {
                    // ++ Path to one item found
                    context.TargetItem = itemsToCheck[indexItemToGrab];
                    context.PathController = tempPaths[indexItemToGrab];
                    PluginLoggerHook.LogDebug?.Invoke($"++M {ai.Npc.playerUsername} CheckForItemsInMap target item {context.TargetItem} {context.TargetItem.transform.position}, valid {context.PathController.IsPathValid()} {context.PathController}");

                    TryPlayNowScavengingVoiceAudio(ai);
                    return CleanAndReturn(context, BehaviourTreeStatus.Success);
                }
            }

            // We checked everything
            if (itemIndex >= count)
            {
                PluginLoggerHook.LogDebug?.Invoke($"??M {ai.Npc.playerUsername} NOTHING more grabbable on map");
                ai.TryPlayCantDoCommandVoiceAudio();
                // todo : stay in place ?
                ai.SetCommandToFollowPlayer(playVoice: false);
                return CleanAndReturn(context, BehaviourTreeStatus.Failure);
            }

            randomIndex = indices[itemIndex];
            //PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} CheckForItemsInMap begin CalculatePathToItem random index = {randomIndex}, itemIndex = {itemIndex}");
            CalculatePathToItem(context, itemsToCheck[randomIndex]);

            return BehaviourTreeStatus.Success;
        }

        private BehaviourTreeStatus CleanAndReturn(BTContext context, BehaviourTreeStatus behaviourTreeStatus)
        {
            itemIndex = 0;
            itemsToCheck.Clear();
            context.nbItemsToCheck = 0;
            return behaviourTreeStatus;
        }

        /// <summary>
        /// Check all object array
        /// </summary>
        /// <returns><c>GrabbableObject</c>GrabbableObject to try to grab</returns>
        private List<GrabbableObject> LookingForItemsToGrabInMap(InternAI ai)
        {
            var items = new List<GrabbableObject>();
            var grabbableObjectsList = InternManager.Instance.GetGrabbableObjectsList();
            for (int i = 0; i < grabbableObjectsList.Count; i++)
            {
                GameObject gameObject = grabbableObjectsList[i];
                if (gameObject == null)
                {
                    continue;
                }

                // Black listed ? 
                if (ai.IsGrabbableObjectBlackListed(gameObject))
                {
                    continue;
                }

                // Get grabbable object infos
                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    continue;
                }

                // Grabbable object ?
                if (!ai.IsGrabbableObjectGrabbable(grabbableObject))
                {
                    continue;
                }

                items.Add(grabbableObject);
            }

            return items;
        }

        private void CalculatePathToItem(BTContext context, GrabbableObject grabbableObject)
        {
            InternAI ai = context.InternAI;

            // Get entrances graph
            GraphController? GraphEntrances = InternManager.Instance.GetGraphEntrances();
            if (GraphEntrances == null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- CheckForItemsInMap GetGraphEntrances not available yet");
                return;
            }

            GraphController tempGraph = new GraphController(GraphEntrances);

            // Add source and dest
            tempGraph.AddPoint(new DJKStaticPoint(Dijkstra.Dijkstra.GetSampledPos(ai.transform.position), $"{ai.Npc.playerUsername} pos"));
            tempGraph.AddPoint(new DJKItemPoint(grabbableObject.transform, ai.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale, grabbableObject.name));

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            List<InstructionParameters> instructions = Dijkstra.Dijkstra.GenerateWorkCalculateNeighbors(tempGraph.DJKPoints);
            List<IInstruction> instructionsToProcess = new List<IInstruction>();
            foreach (var instrParams in instructions)
            {
                instructionsToProcess.Add(instrParams.targetDJKPoint.GenerateInstruction(idBatch, instrParams));
            }

            tempGraphs[randomIndex] = tempGraph;
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
        }

        private void OnBatchCompleted()
        {
            // log
            //PluginLoggerHook.LogDebug?.Invoke($"CheckForItemsToGrabInMap itemIndex {itemIndex}, random i {randomIndex} ------- {tempGraphs[randomIndex]}");

            // Get full path
            PathController pathCalculated = new PathController();
            pathCalculated.SetNewPath(Dijkstra.Dijkstra.CalculatePath(tempGraphs[randomIndex].DJKPoints));
            tempPaths[randomIndex] = pathCalculated;

            // log
            //PluginLoggerHook.LogDebug?.Invoke($"CheckForItemsToGrabInMap itemIndex {itemIndex}, random i {randomIndex} valid {pathCalculated.IsPathValid()} ======= {pathCalculated.GetFullPathString()}");

            itemIndex++;
        }

        private int GetIndexMinPath()
        {
            int indexBestPath = -1;
            float minDist = float.MaxValue;
            for (int i = 0; i < tempPaths.Length; i++)
            {
                PathController tempPath = tempPaths[i];
                if (tempPath == null || !tempPath.IsPathValid())
                {
                    continue;
                }

                float dist = tempPath.GetFullPathDistance();
                if (dist < minDist)
                {
                    minDist = dist;
                    indexBestPath = i;
                }
            }
            return indexBestPath;
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
