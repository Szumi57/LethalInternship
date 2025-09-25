using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGraphEntrances
    {
        private List<IDJKPoint>? DJKPointsGraph = null!;
        private List<IDJKPoint>? TempDJKPointsGraph = null!;

        private long timer = 10000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        private bool IsCalculating = false;

        public List<IDJKPoint>? GetGraphEntrances()
        {
            if (!NeedToRecalculate()
                || IsCalculating)
            {
                if (IsCalculating)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"CalculateGraphEntrances Calculating");
                }

                CleanNeighbors();
                return DJKPointsGraph;
            }

            // Construct graph entrances
            EntranceTeleport[] entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            TempDJKPointsGraph = CalculateGraphEntrances(entrancesTeleportArray);

            // Calculate Neighbors
            Dijkstra.Dijkstra.CalculateNeighborsDeferred(TempDJKPointsGraph, idBatch: -1, OnBatchComplete);

            return DJKPointsGraph;
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

        private void OnBatchComplete()
        {
            DJKPointsGraph = new List<IDJKPoint>(TempDJKPointsGraph);
            IsCalculating = false;
        }
    }
}
