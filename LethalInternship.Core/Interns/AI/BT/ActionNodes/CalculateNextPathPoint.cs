using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.Dijkstra.PathRequests;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CalculateNextPathPoint : IBTAction
    {
        private BTContext currentContext = null!;
        private List<IDJKPoint> DJKPointsGraph = null!;
        private List<PathResponse> pendingPaths = new List<PathResponse>();
        private int nbRequestsAsked;

        private TimedCalculatePath calculateDestinationPathTimed = new TimedCalculatePath();
        private TimedCalculatePath calculateNextPointPathTimed = new TimedCalculatePath();

        private DJKPositionPoint source = null!;
        private DJKPositionPoint dest = null!;

        private long timerCalculatePathPartial = 3000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public BehaviourTreeStatus Action(BTContext context)
        {
            currentContext = context;
            InternAI ai = context.InternAI;
            if (!ai.IsAgentInValidState())
            {
                return BehaviourTreeStatus.Success;
            }

            // Check if destination reachable
            TimedCalculatePathResponse path = calculateDestinationPathTimed.GetPath(ai, context.PathController.GetDestination().GetClosestPointFrom(ai.transform.position));
            if (path.PathStatus == NavMeshPathStatus.PathComplete)
            {
                // Go directly to destination
                //PluginLoggerHook.LogDebug?.Invoke($"- Destination reachable");
                context.PathController.SetNextPointToDestination();
                return BehaviourTreeStatus.Success;
            }

            // Check if current PathPoint reachable
            path = calculateNextPointPathTimed.GetPath(ai, context.PathController.GetCurrentPoint(ai.transform.position, getRealCurrentPoint: true));
            if (!path.IsDirectlyReachable)
            {
                // Need to calculate further
                CalculateGraphPath(context);
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathComplete
                || (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete))
            {
                if (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status {path.PathStatus} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                }

                // Go
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathPartial)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- ?? next point to partial path");
                context.PathController.SetCurrentPoint(path.Path.corners[path.Path.corners.Length - 1]);

                // Try to still calculate
                long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
                if (elapsedTime > timerCalculatePathPartial)
                {
                    lastTimeCalculate = DateTime.Now.Ticks;
                    CalculateGraphPath(context);
                }

                return BehaviourTreeStatus.Success;
            }

            // Need to calculate further
            CalculateGraphPath(context);
            return BehaviourTreeStatus.Success;
        }

        private void CalculateGraphPath(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Get entrances graph
            List<IDJKPoint>? GraphEntrances = InternManager.Instance.GetGraphEntrances();
            if (GraphEntrances == null || GraphEntrances.Count == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- GetGraphEntrances not available yet/empty");
                return;
            }

            DJKPointsGraph = new List<IDJKPoint>(GraphEntrances);

            // Add source and dest
            int id = DJKPointsGraph.Count;
            source = new DJKPositionPoint(id++, ai.transform.position, "Intern pos");
            dest = new DJKPositionPoint(id++, context.PathController.GetDestination().GetClosestPointFrom(ai.transform.position), "Destination");
            DJKPointsGraph.Add(source);
            DJKPointsGraph.Add(dest);

            // Calculate Neighbors
            pendingPaths.Clear();
            nbRequestsAsked = Dijkstra.Dijkstra.CalculateNeighbors(DJKPointsGraph, idBatch: (int)ai.Npc.playerClientId, OnPathCalculated);
        }

        private void OnPathCalculated(PathResponse pathResponse)
        {
            pendingPaths.Add(pathResponse);
            if (pendingPaths.Count >= nbRequestsAsked)
            {
                ProcessPendingPathCalculated();
            }
        }

        private void ProcessPendingPathCalculated()
        {
            foreach (var pathResponse in pendingPaths)
            {
                if (pathResponse.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    float distance = Dijkstra.Dijkstra.GetFullDistancePath(pathResponse.path);
                    distance = distance < 1 ? 1 : distance;
                    pathResponse.startDJKPoint.TryAddToNeighbors(pathResponse.targetDJKPoint, distance);
                    pathResponse.targetDJKPoint.TryAddToNeighbors(pathResponse.startDJKPoint, distance);
                }
                else if (pathResponse.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    float distance = Dijkstra.Dijkstra.GetFullDistancePath(pathResponse.path);
                    distance += 100000000f;
                    pathResponse.startDJKPoint.TryAddToNeighbors(pathResponse.targetDJKPoint, distance);
                    pathResponse.targetDJKPoint.TryAddToNeighbors(pathResponse.startDJKPoint, distance);
                }
            }

            // log
            PluginLoggerHook.LogDebug?.Invoke($"------- {currentContext.PathController.GetGraphString(DJKPointsGraph)}");

            // Get full path
            currentContext.PathController.SetNewPath(Dijkstra.Dijkstra.CalculatePath(DJKPointsGraph, source, dest));

            // log
            PluginLoggerHook.LogDebug?.Invoke($"======= {currentContext.PathController.GetPathString()}");
        }
    }
}
