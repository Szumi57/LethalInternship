using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForItemsInRange : IBTAction
    {
        private int itemIndex = 0;
        private List<GrabbableObject> itemsToCheck = new List<GrabbableObject>();
        private GrabbableObject? itemToGrabInRange = null;

        private List<PathController> tempPaths = new List<PathController>();
        private List<PathfindingContext> tempPfs = new List<PathfindingContext>();
        private List<int> pathIds = new List<int>();

        private readonly List<IInstruction> instructionsToProcess = new List<IInstruction>(1024);

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (itemToGrabInRange != null)
            {
                if (InternManager.Instance.IsGrabbableObjectGrabbable(itemToGrabInRange))
                    return BehaviourTreeStatus.Success;

                Debug.Log($"--------- {ai.Npc.playerUsername} CheckForItemsInRange itemToGrabInRange {itemToGrabInRange} not grabbable !!!! itemsToCheck.Count {itemsToCheck.Count}");
                itemsToCheck.Clear();
            }

            if (itemsToCheck.Count == 0)
            {
                CleanTempLists();
                itemToGrabInRange = null;

                LookingForItemsToGrabInRange(ai);
                Vector3 aiPos = ai.transform.position;
                itemsToCheck.Sort((a, b) => // Sort without linq
                {
                    float da = (a.transform.position - aiPos).sqrMagnitude;
                    float db = (b.transform.position - aiPos).sqrMagnitude;
                    return da.CompareTo(db);
                });

                if (itemsToCheck.Count == 0)
                {
                    return BehaviourTreeStatus.Failure;
                }
            }

            if (itemToGrabInRange == null
                && itemsToCheck.Count > 0)
            {
                int indexItemToGrab = GetIndexMinPath();
                if (indexItemToGrab >= 0)
                {
                    // ++ Path to one item found
                    context.TargetItem = itemsToCheck[indexItemToGrab];
                    itemToGrabInRange = context.TargetItem;
                    context.PathfindingContext.CopyFrom(tempPfs[indexItemToGrab]);
                    context.PathController.CopyFrom(tempPaths[indexItemToGrab]);
                    PluginLoggerHook.LogDebug?.Invoke($"++R {ai.Npc.playerUsername} CheckForItemsInRange target item {context.TargetItem} {context.TargetItem.transform.position}, valid {context.PathController.IsPathValid()} {context.PathfindingContext.GetFullPathString(context.PathController.PathIds)} {context.PathfindingContext.Destination}");
                    TryPlayFoundLootVoiceAudio(ai);

                    itemIndex = 0;
                    itemsToCheck.Clear();
                    return BehaviourTreeStatus.Success;
                }
            }

            // We checked everything in range
            if (itemIndex >= itemsToCheck.Count)
            {
                itemIndex = 0;
                itemsToCheck.Clear();
                return BehaviourTreeStatus.Failure;
            }

            CalculatePathToItem(context, itemsToCheck[itemIndex]);

            return BehaviourTreeStatus.Success;
        }

        /// <summary>
        /// Check all object array
        /// if intern is close and can see an item to grab.
        /// </summary>
        /// <returns><c>GrabbableObject</c>GrabbableObject to try to grab</returns>
        private void LookingForItemsToGrabInRange(InternAI ai)
        {
            itemsToCheck.Clear();
            var grabbableObjectsList = InternManager.Instance.GetGrabbableObjectsList();
            for (int i = 0; i < grabbableObjectsList.Count; i++)
            {
                GameObject gameObject = grabbableObjectsList[i];
                if (gameObject == null)
                {
                    continue;
                }

                // Object not outside when ai inside and vice versa
                Vector3 gameObjectPosition = gameObject.transform.position;
                if (ai.isOutside && gameObjectPosition.y < -100f)
                {
                    continue;
                }
                else if (!ai.isOutside && gameObjectPosition.y > -80f)
                {
                    continue;
                }

                // Object in range ?
                float sqrDistanceEyeGameObject = (gameObjectPosition - ai.eye.position).sqrMagnitude;
                if (sqrDistanceEyeGameObject > Const.INTERN_OBJECT_RANGE * Const.INTERN_OBJECT_RANGE)
                {
                    continue;
                }

                // Black listed ? 
                if (InternManager.Instance.IsGrabbableObjectBlackListed(gameObject))
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
                if (!InternManager.Instance.IsGrabbableObjectGrabbable(grabbableObject))
                {
                    continue;
                }

                // Object close to awareness distance ?
                DrawUtil.DrawLine(ai.LineRendererUtil.GetLineRenderer(), ai.eye.position, gameObjectPosition, Color.green);
                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_AWARNESS * Const.INTERN_OBJECT_AWARNESS)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"awareness {grabbableObject.name}");
                }
                // Object visible ?
                else if (!Physics.Linecast(ai.eye.position, gameObjectPosition, 134217984))
                {
                    Vector3 to = gameObjectPosition - ai.eye.position;
                    if (Vector3.Angle(ai.eye.forward, to) < Const.INTERN_FOV)
                    {
                        // Object in FOV
                        PluginLoggerHook.LogDebug?.Invoke($"LOS {grabbableObject.name}");
                    }
                    else
                    {
                        // Object not in FOV
                        continue;
                    }
                }
                else
                {
                    // Object not in line of sight
                    continue;
                }

                itemsToCheck.Add(grabbableObject);
            }
        }

        private void CalculatePathToItem(BTContext context, GrabbableObject grabbableObject)
        {
            InternAI ai = context.InternAI;

            PathfindingContext pf = GetNewPathfindingContext(itemIndex);
            pf.Clear();
            pf.SharedGraph.CopyFrom(InternManager.Instance.GetGraphEntrances());

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
                pf.StartNeighbors.Add(new DJKNeighbor(to, targetPos, dist));
            };
            NeighborResult destinationWriter = (from, to, startPos, targetPos, dist) =>
            {
                pf.SharedGraph.Neighbors[from].Add(new DJKNeighbor(to, targetPos, dist + Const.PENALTY_ENTRANCE));
            };

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            Dijkstra.Dijkstra.GenerateNeighborInstructions(pf, idBatch, startWriter, destinationWriter, instructionsToProcess);
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
        }

        private void OnBatchCompleted()
        {
            // Get full path
            PathController pathCalculated = GetNewPathController(itemIndex);
            pathCalculated.Reset();

            PathfindingContext pf = tempPfs[itemIndex];
            Dijkstra.Dijkstra.CalculatePath(pf,
                                            pf.Start.Id,
                                            pf.Destination.Id,
                                            pathIds);
            pathCalculated.SetNewPath(pathIds);

            // log
            PluginLoggerHook.LogDebug?.Invoke($"=> CheckForItemsToGrabInRange OnBatchCompleted >>> {tempPfs[itemIndex].GetFullPathString(pathCalculated.PathIds)} | Destination {tempPfs[itemIndex].Destination}");
            PluginLoggerHook.LogDebug?.Invoke($"=> CheckForItemsToGrabInRange OnBatchCompleted itemIndex={itemIndex} tempPfs[{itemIndex}] {tempPfs[itemIndex]}");

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

        private void TryPlayFoundLootVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FoundLoot,
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
