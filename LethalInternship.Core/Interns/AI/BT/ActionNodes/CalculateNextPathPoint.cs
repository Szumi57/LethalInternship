using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CalculateNextPathPoint : IBTAction
    {
        private NavMeshPath path = null!;

        private TimedCalculatePath calculateDestinationPathTimed = new TimedCalculatePath();
        private TimedCalculatePath calculateNextPointPathTimed = new TimedCalculatePath();

        private Coroutine? calculateNeighborsCoroutine = null!;
        private CalculateNeighborsParameters calculateNeighborsParameters = null!;
        private DJKPositionPoint source = null!;
        private DJKPositionPoint dest = null!;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            if (!ai.IsAgentInValidState())
            {
                return BehaviourTreeStatus.Success;
            }

            // Check if destination reachable
            path = calculateDestinationPathTimed.GetPath(ai, context.PathController.GetDestination().GetClosestPointFrom(ai.transform.position));
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                // Go directly to destination
                //PluginLoggerHook.LogDebug?.Invoke($"- Destination reachable");
                context.PathController.SetNextPointToDestination();
                return BehaviourTreeStatus.Success;
            }
            
            // Check if current PathPoint reachable
            path = calculateNextPointPathTimed.GetPath(ai, context.PathController.GetCurrentPoint(ai.transform.position, getRealCurrentPoint: true));
            if (path.status == NavMeshPathStatus.PathComplete
                || (path.status == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete))
            {
                if (path.status == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status force {path.status} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                }

                // Go
                context.PathController.SetCurrentPointToReachable();
                return BehaviourTreeStatus.Success;
            }
            else if (path.status == NavMeshPathStatus.PathPartial)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- ?? next point to partial path");
                context.PathController.SetNextPartialPoint(path.corners[path.corners.Length - 1]);
            }

            // Path partial, need to calculate further
            CalculateGraphPath(context);

            return BehaviourTreeStatus.Success;
        }

        private void CalculateGraphPath(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (calculateNeighborsCoroutine == null)
            {
                // Get entrances graph
                List<IDJKPoint>? GraphEntrances = InternManager.Instance.GetGraphEntrances();
                if (GraphEntrances == null)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"- GetGraphEntrances not available yet, SetNextPointToDestination");
                    //context.PathController.SetNextPointToDestination();
                    return;
                }

                List<IDJKPoint> DJKPointsGraph = new List<IDJKPoint>(GraphEntrances);

                // Add source and dest
                int id = DJKPointsGraph.Count;
                source = new DJKPositionPoint(id++, ai.transform.position, "Intern pos");
                dest = new DJKPositionPoint(id++, context.PathController.GetDestination().GetClosestPointFrom(ai.transform.position), "Destination");
                DJKPointsGraph.Add(source);
                DJKPointsGraph.Add(dest);

                calculateNeighborsParameters = new CalculateNeighborsParameters(DJKPointsGraph);

                // Calculate Neighbors
                PluginLoggerHook.LogDebug?.Invoke($"- CalculateGraphPath Calculating Neighbors ...");
                calculateNeighborsCoroutine = ai.StartCoroutine(Dijkstra.Dijkstra.CalculateNeighbors(calculateNeighborsParameters));
            }

            if (calculateNeighborsParameters.CalculateFinished)
            {
                List<IDJKPoint>? DJKPointsGraph = calculateNeighborsParameters.DJKPointsGraph;
                calculateNeighborsCoroutine = null;

                // log
                PluginLoggerHook.LogDebug?.Invoke($"------- {context.PathController.GetGraphString(DJKPointsGraph)}");

                // Get full path
                context.PathController.SetNewPath(Dijkstra.Dijkstra.CalculatePath(DJKPointsGraph, source, dest));

                // log
                PluginLoggerHook.LogDebug?.Invoke($"======= {context.PathController.GetPathString()}");
                return;
            }
        }

        public class TimedCalculatePath
        {
            private NavMeshPath path = new NavMeshPath();

            private Vector3? previousDestination;
            private Vector3? currentDestination;

            private long timer = 500 * TimeSpan.TicksPerMillisecond;
            private long lastTimeCalculate;

            public NavMeshPath GetPath(InternAI internAI, Vector3 destination, bool force = false)
            {
                if (path == null)
                {
                    path = new NavMeshPath();
                }

                if (NeedToRecalculate(destination) || force)
                {
                    CalculatePath(internAI, destination);
                    return path;
                }

                return path;
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
                NavMesh.CalculatePath(internAI.transform.position, destination, NavMesh.AllAreas, path);
            }
        }
    }
}
