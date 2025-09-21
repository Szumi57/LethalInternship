using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.Dijkstra.PathRequests;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using UnityEngine;
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
                    PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status force {path.PathStatus} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                }

                // Go
                context.PathController.SetCurrentPointToReachable();
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

        private void DrawPath(LineRendererUtil lineRendererUtil, NavMeshPath path)
        {
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i + 1], Color.red);
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i] + new Vector3(0, 1, 0), Color.red);
                }
            }
            else if (path.status == NavMeshPathStatus.PathComplete)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i + 1], Color.white);
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i] + new Vector3(0, 1, 0), Color.white);
                }
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"DrawPath PathInvalid");
            }
        }

        public struct TimedCalculatePathResponse
        {
            public NavMeshPathStatus PathStatus;
            public NavMeshPath Path;
            public bool IsDirectlyReachable;

            public TimedCalculatePathResponse(NavMeshPathStatus pathStatus, NavMeshPath? path, bool isDirectlyReachable)
            {
                PathStatus = pathStatus;
                Path = path == null ? new NavMeshPath() : path;
                IsDirectlyReachable = isDirectlyReachable;
            }
        }

        public class TimedCalculatePath
        {
            private TimedCalculatePathResponse result = new TimedCalculatePathResponse();
            private NavMeshPath path = new NavMeshPath();

            private Vector3? previousDestination;
            private Vector3? currentDestination;

            private long timer = 1000 * TimeSpan.TicksPerMillisecond;
            private long lastTimeCalculate;

            public TimedCalculatePathResponse GetPath(InternAI internAI, Vector3 destination, bool force = false)
            {
                if (NeedToRecalculate(destination) || force)
                {
                    CalculatePath(internAI, destination);
                    return result;
                }

                return result;
            }

            private bool NeedToRecalculate(Vector3 destination)
            {
                long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;

                previousDestination = currentDestination;
                currentDestination = destination;
                if (currentDestination != previousDestination)
                {
                    return true;
                }

                if (elapsedTime > timer)
                {
                    lastTimeCalculate = DateTime.Now.Ticks;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void CalculatePath(InternAI internAI, Vector3 destination)
            {
                Vector3 start = internAI.transform.position;
                if (Mathf.Abs(start.y - destination.y) > 100f)
                {
                    result = new TimedCalculatePathResponse(NavMeshPathStatus.PathInvalid, path: null, isDirectlyReachable: false);
                    return;
                }

                NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, path);
                result = new TimedCalculatePathResponse(path.status, path, isDirectlyReachable: true);
            }
        }
    }
}
