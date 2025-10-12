using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForItemsToGrab : IBTAction
    {
        private int itemIndex = 0;
        private List<GrabbableObject> itemsToCheck = new List<GrabbableObject>();
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

            if (itemIndex == 0)
            {
                itemsToCheck = LookingForItemsToGrab(ai);
                if (itemsToCheck.Count == 0)
                {
                    context.TargetItem = null;
                    return BehaviourTreeStatus.Success;
                }

                tempGraphs = new GraphController[itemsToCheck.Count];
                tempPaths = new PathController[itemsToCheck.Count];
            }

            if (itemIndex >= itemsToCheck.Count)
            {
                itemIndex = 0;
                int indexItemToGrab = GetIndexMinPath();
                if(indexItemToGrab < 0)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"-- no path to no items found");
                    return BehaviourTreeStatus.Success;
                }

                // Path to one item found
                context.TargetItem = itemsToCheck[indexItemToGrab];
                context.PathController = tempPaths[indexItemToGrab];
                return BehaviourTreeStatus.Success;
            }

            CalculatePathToItem(context, itemsToCheck[itemIndex]);

            return BehaviourTreeStatus.Success;
        }

        /// <summary>
        /// Check all object array
        /// if intern is close and can see an item to grab.
        /// </summary>
        /// <returns><c>GrabbableObject</c>GrabbableObject to try to grab</returns>
        private List<GrabbableObject> LookingForItemsToGrab(InternAI ai)
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
                if (IsGrabbableObjectBlackListed(gameObject))
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

                items.Add(grabbableObject);
            }

            return items;
        }

        private bool IsGrabbableObjectBlackListed(GameObject gameObjectToEvaluate)
        {
            // Bee nest
            if (!PluginRuntimeProvider.Context.Config.GrabBeesNest
                && gameObjectToEvaluate.name.Contains("RedLocustHive"))
            {
                return true;
            }

            // Dead bodies
            if (!PluginRuntimeProvider.Context.Config.GrabDeadBodies
                && gameObjectToEvaluate.name.Contains("RagdollGrabbableObject")
                && gameObjectToEvaluate.tag == "PhysicsProp"
                && gameObjectToEvaluate.GetComponentInParent<DeadBodyInfo>() != null)
            {
                return true;
            }

            // Maneater
            if (!PluginRuntimeProvider.Context.Config.GrabManeaterBaby
                && gameObjectToEvaluate.name.Contains("CaveDwellerEnemy"))
            {
                return true;
            }

            // Wheelbarrow
            if (!PluginRuntimeProvider.Context.Config.GrabWheelbarrow
                && gameObjectToEvaluate.name.Contains("Wheelbarrow"))
            {
                return true;
            }

            // ShoppingCart
            if (!PluginRuntimeProvider.Context.Config.GrabShoppingCart
                && gameObjectToEvaluate.name.Contains("ShoppingCart"))
            {
                return true;
            }

            // Baby kiwi egg
            if (!PluginRuntimeProvider.Context.Config.GrabKiwiBabyItem
                && gameObjectToEvaluate.name.Contains("KiwiBabyItem"))
            {
                return true;
            }

            return false;
        }

        private void CalculatePathToItem(BTContext context, GrabbableObject grabbableObject)
        {
            InternAI ai = context.InternAI;

            // Get entrances graph
            GraphController? GraphEntrances = InternManager.Instance.GetGraphEntrances();
            if (GraphEntrances == null || GraphEntrances.DJKPoints.Count == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- GetGraphEntrances not available yet/empty");
                return;
            }

            GraphController tempGraph = new GraphController(GraphEntrances);

            // Add source and dest
            tempGraph.AddPoint(new DJKStaticPoint(Dijkstra.Dijkstra.GetSampledPos(ai.transform.position), "Intern pos"));
            tempGraph.AddPoint(new DJKItemPoint(grabbableObject.transform, ai.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale, grabbableObject.name));

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            List<InstructionParameters> instructions = Dijkstra.Dijkstra.GenerateWorkCalculateNeighbors(tempGraph.DJKPoints);
            List<IInstruction> instructionsToProcess = new List<IInstruction>();
            foreach (var instrParams in instructions)
            {
                instructionsToProcess.Add(instrParams.targetDJKPoint.GenerateInstruction(idBatch, instrParams));
            }

            tempGraphs[itemIndex] = tempGraph;
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
        }

        private void OnBatchCompleted()
        {
            // log
            //PluginLoggerHook.LogDebug?.Invoke($"CheckForItemsToGrab itemIndex {itemIndex} ------- {tempGraphs[itemIndex]}");

            // Get full path
            PathController pathCalculated = new PathController();
            pathCalculated.SetNewPath(Dijkstra.Dijkstra.CalculatePath(tempGraphs[itemIndex].DJKPoints));
            tempPaths[itemIndex] = pathCalculated;

            // log
            PluginLoggerHook.LogDebug?.Invoke($"CheckForItemsToGrab itemIndex {itemIndex} ======= {tempPaths[itemIndex].GetFullPathString()}");

            itemIndex++;
        }

        private int GetIndexMinPath()
        {
            int indexBestPath = -1;
            float minDist = float.MaxValue;
            for (int i = 0; i < tempPaths.Length; i++)
            {
                PathController tempPath = tempPaths[i];
                if (tempPath.IsPathNotValid())
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
    }
}
