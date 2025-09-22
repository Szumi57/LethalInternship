using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.Dijkstra.PathRequests;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGraphEntrances
    {
        private List<IDJKPoint>? DJKPointsGraph = null!;
        private List<PathResponse> pendingPaths = new List<PathResponse>();
        private int nbRequestsAsked;

        private long timer = 10000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public List<IDJKPoint>? GetGraphEntrances(MonoBehaviour coroutineLauncher)
        {
            if (DJKPointsGraph == null)
            {
                DJKPointsGraph = new List<IDJKPoint>();
            }

            if (!NeedToRecalculate())
            {
                CleanNeighbors();
                PluginLoggerHook.LogDebug?.Invoke($"- TimedGetGraphEntrances return cache");
                return DJKPointsGraph;
            }

            // Construct graph entrances
            EntranceTeleport[] entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            DJKPointsGraph = CalculateGraphEntrances(entrancesTeleportArray);

            // Calculate Neighbors
            pendingPaths.Clear();
            nbRequestsAsked = Dijkstra.Dijkstra.CalculateNeighbors(DJKPointsGraph, idBatch: -1, OnPathCalculated);
            PluginLoggerHook.LogDebug?.Invoke($"- TimedGetGraphEntrances nbRequestsAsked {nbRequestsAsked} calculating...");

            return null;
        }

        private bool NeedToRecalculate()
        {
            long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
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

        private void CleanNeighbors()
        {
            if (DJKPointsGraph == null)
            {
                return;
            }

            List<int> neighborsPresent = DJKPointsGraph.Select(x => x.Id).ToList();
            foreach (var point in DJKPointsGraph)
            {
                point.Neighbors.RemoveAll(n => !neighborsPresent.Contains(n.neighbor.Id));
            }
        }

        private List<IDJKPoint> CalculateGraphEntrances(EntranceTeleport[] entrancesTeleportArray)
        {
            List<IDJKPoint> DJKPointsGraphEntrances = new List<IDJKPoint>();

            // List<DJKPoint> init with entrances
            int id = 0;
            foreach (var entrance in entrancesTeleportArray)
            {
                bool newDJKPoint = true;
                foreach (var DJKP in DJKPointsGraphEntrances)
                {
                    if (((DJKEntrancePoint)DJKP).TryAddOtherEntrance(entrance))
                    {
                        newDJKPoint = false;
                        break;
                    }
                }

                if (newDJKPoint)
                {
                    DJKPointsGraphEntrances.Add(new DJKEntrancePoint(id++, entrance));
                }
            }

            return DJKPointsGraphEntrances;
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
                    pathResponse.startDJKPoint.TryAddToNeighbors(pathResponse.targetDJKPoint, distance);
                    pathResponse.targetDJKPoint.TryAddToNeighbors(pathResponse.startDJKPoint, distance);
                }
            }
        }
    }
}
