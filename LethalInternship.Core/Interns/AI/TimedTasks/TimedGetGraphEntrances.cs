using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGraphEntrances
    {
        private EntranceTeleport[] entrancesTeleportArray = null!;
        private Dictionary<EntranceTeleport, DJKEntrancePoint> dictEntrancesDJKPoint = new Dictionary<EntranceTeleport, DJKEntrancePoint>();

        private GraphController currentGraph = new GraphController(256);
        private GraphController buildingGraph = new GraphController(256);

        private readonly List<IInstruction> instructionsToProcess = new List<IInstruction>(1024);

        private float timer = 10f;
        private float nextCheckTime;

        private bool IsCalculating = false;

        public GraphController GetGraphEntrances()
        {
            if (!IsCalculating && NeedToRecalculate())
            {
                StartRebuild();
            }

            return currentGraph;
        }

        private bool NeedToRecalculate()
        {
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }
            return false;
        }

        private void StartRebuild()
        {
            buildingGraph.Clear();

            entrancesTeleportArray = Object.FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            CalculateGraphEntrances(buildingGraph);

            CalculateNeighbors(buildingGraph);

            IsCalculating = true;
        }

        private GraphController CalculateGraphEntrances(GraphController graph)
        {
            graph.Clear();
            // init with entrances
            foreach (var entrance in entrancesTeleportArray)
            {
                bool newDJKPoint = true;
                foreach (var DJKP in graph.Points)
                {
                    if (((DJKEntrancePoint)DJKP).TryAddOtherEntrance(entrance))
                    {
                        newDJKPoint = false;
                        break;
                    }
                }

                if (newDJKPoint)
                {
                    if (!dictEntrancesDJKPoint.TryGetValue(entrance, out var point))
                    {
                        point = new DJKEntrancePoint(entrance);
                        dictEntrancesDJKPoint.Add(entrance, point);
                    }
                    graph.AddPoint(point);
                }
            }
            return graph;
        }

        private void CalculateNeighbors(GraphController graphToCalculate)
        {
            NeighborResult graphWriter = (from, to, startPos, targetPos, dist) =>
            {
                Debug.Log($"adding neighbors to grah : from {from} to {to} pos {targetPos}");
                graphToCalculate.Neighbors[from].Add(new DJKNeighbor(to, targetPos, dist));
                Debug.Log($"adding neighbors to grah : from {to} to {from} pos {startPos}");
                graphToCalculate.Neighbors[to].Add(new DJKNeighbor(from, startPos, dist));
            };

            int idBatch = -1;
            Dijkstra.Dijkstra.GenerateNeighborInstructions(graphToCalculate.Points, idBatch, graphWriter, instructionsToProcess);
            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchComplete);
            IsCalculating = true;
        }

        private void OnBatchComplete()
        {
            SwapGraphs();
            IsCalculating = false;
        }

        private void SwapGraphs()
        {
            (currentGraph, buildingGraph) = (buildingGraph, currentGraph);
        }
    }
}
