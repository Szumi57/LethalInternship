using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CalculateNextPathPoint : IBTAction
    {
        private BTContext currentContext = null!;

        private List<int> pathIds = new List<int>();

        private TimedCalculatePath calculateDestinationPathTimed = new TimedCalculatePath();
        private TimedCalculatePath calculateNextPointPathTimed = new TimedCalculatePath();

        private readonly List<IInstruction> instructionsToProcess = new List<IInstruction>(1024);

        public BehaviourTreeStatus Action(BTContext context)
        {
            currentContext = context;
            InternAI ai = context.InternAI;
            if (!ai.IsAgentInValidState())
            {
                return BehaviourTreeStatus.Success;
            }

            // Check if destination reachable
            if (context.PathfindingContext.Destination == null)
            {
                CalculatePath(context);
                return BehaviourTreeStatus.Success;
            }

            TimedCalculatePathResponse path;
            path = calculateDestinationPathTimed.GetPath(ai, context.PathfindingContext.Destination.GetClosestPointTo(ai.transform.position));

            if (path.PathStatus == NavMeshPathStatus.PathComplete)
            {
                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);
                // Go directly to destination
                context.PathController.SetNextPointToDestination();
                return BehaviourTreeStatus.Success;
            }

            // Check if current PathPoint reachable
            path = calculateNextPointPathTimed.GetPath(ai, context.PathfindingContext.GetCurrentTargetPos(context.PathController.IndexCurrentPoint,
                                                                                                          context.PathController.PathIds,
                                                                                                          ai.transform.position));
            if (!path.IsDirectlyReachable)
            {
                // Need to calculate further
                //Debug.Log($"CalculatePath !path.IsDirectlyReachable {context.PathController.GetCurrentPointPos(ai.transform.position)}");
                CalculatePath(context);
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathComplete
                 || (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete))
            {
                // Path calculated invalid but agent path still valid

                //if (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete)
                //{
                //    PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status {path.PathStatus} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                //}

                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);

                // Go
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathPartial)
            {
                // Path calculated partial
                DJKStaticPoint dJKPointPartial = InternManager.Instance.Pools.Get<DJKStaticPoint>();
                dJKPointPartial.Position = path.Path.corners[^1];
                dJKPointPartial.Name = "PartialPoint";
                context.PathfindingContext.SetDestination(dJKPointPartial.Clone(InternManager.Instance.Pools));

                // Try to still calculate
                if (!context.PathController.IsPathValid())
                {
                    //Debug.Log($"CalculatePath PathStatus == NavMeshPathStatus.PathPartial");
                    CalculatePath(context);
                }

                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);

                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathInvalid
                  && ai.agent.path.status == NavMeshPathStatus.PathPartial
                  && path.Path.corners.Length > 0)
            {
                // Path calculated invalid but agent path partial
                DJKStaticPoint dJKPointPartial = InternManager.Instance.Pools.Get<DJKStaticPoint>();
                dJKPointPartial.Position = path.Path.corners[^1];
                dJKPointPartial.Name = "PartialPoint";
                context.PathfindingContext.SetDestination(dJKPointPartial.Clone(InternManager.Instance.Pools));

                // Try to still calculate
                if (!context.PathController.IsPathValid())
                {
                    //Debug.Log($"CalculatePath avMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathPartial");
                    CalculatePath(context);
                }

                DrawUtil.DrawPath(ai.LineRendererUtil, ai.agent.path);

                return BehaviourTreeStatus.Success;
            }

            // Need to calculate further
            //Debug.Log($"end CalculatePath path.PathStatus {path.PathStatus} , ai.agent.path.status {ai.agent.path.status}  {context.PathController.GetCurrentPointPos(ai.transform.position)}");
            CalculatePath(context);
            return BehaviourTreeStatus.Success;
        }

        private void CalculatePath(BTContext context)
        {
            InternAI ai = context.InternAI;

            var pf = context.PathfindingContext;
            pf.Clear(clearDest: false);
            pf.SharedGraph.CopyFrom(InternManager.Instance.GetGraphEntrances());

            // Add source and dest
            DJKStaticPoint dJKPointStart = InternManager.Instance.Pools.Get<DJKStaticPoint>();
            dJKPointStart.Position = Dijkstra.Dijkstra.GetSampledPos(ai.transform.position);
            dJKPointStart.Name = $"{ai.Npc.playerUsername} pos";
            pf.SetStart(dJKPointStart);
            // Destination 
            if (context.PathfindingContext.Destination == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{ai.Npc.playerUsername} CalculateNextPathPoint SetDestination context.PathfindingContext.Destination == null");
            }

            NeighborResult startWriter = (from, to, startPos, targetPos, dist) =>
            {
                //Debug.Log($"{ai.Npc.playerUsername} CalculateNextPathPoint adding neighbors to star : from {to} to {from} startPos {startPos} targetPos {targetPos} dist {dist}");
                pf.StartNeighbors.Add(new DJKNeighbor(to, targetPos, dist));
            };
            NeighborResult destinationWriter = (from, to, startPos, targetPos, dist) =>
            {
                //Debug.Log($"{ai.Npc.playerUsername} CalculateNextPathPoint adding neighbors to dest : from {to} to {from} startPos {startPos} targetPos {targetPos} dist {dist} + {Const.PENALTY_ENTRANCE}");
                pf.SharedGraph.Neighbors[from].Add(new DJKNeighbor(to, targetPos, dist + Const.PENALTY_ENTRANCE));
            };

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            Dijkstra.Dijkstra.GenerateNeighborInstructions(pf, idBatch, startWriter, destinationWriter, instructionsToProcess);
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
            PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} CalculateNextPathPoint begin CalculatePathToDest {pf.Destination}");
        }

        private void OnBatchCompleted()
        {
            // Get full path
            Dijkstra.Dijkstra.CalculatePath(currentContext.PathfindingContext,
                                            currentContext.PathfindingContext.Start.Id,
                                            currentContext.PathfindingContext.Destination.Id,
                                            pathIds);
            currentContext.PathController.SetNewPath(pathIds);

            // log
            PluginLoggerHook.LogDebug?.Invoke($"=> {currentContext.InternAI.Npc.playerUsername} CalculateNextPathPoint OnBatchCompleted >>> {currentContext.PathfindingContext.GetFullPathString(currentContext.PathController.PathIds)} | Destination {currentContext.PathfindingContext.Destination}");
            PluginLoggerHook.LogDebug?.Invoke($"=> {currentContext.InternAI.Npc.playerUsername} CalculateNextPathPoint OnBatchCompleted {currentContext.PathfindingContext}");
        }
    }
}
