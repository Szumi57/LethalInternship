using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGraphEntrances
    {
        private EntranceTeleport[] entrancesTeleportArray = null!;

        private GraphController currentGraph = new GraphController(Const.GRAPH_CAPACITY);
        private GraphController buildingGraph = new GraphController(Const.GRAPH_CAPACITY);

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
            entrancesTeleportArray = Object.FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            CalculateGraphEntrances(buildingGraph);

            CalculateNeighbors(buildingGraph);

            IsCalculating = true;
        }

        private GraphController CalculateGraphEntrances(GraphController graph)
        {
            graph.Reset();

            foreach (var entrance in entrancesTeleportArray)
            {
                var point = InternManager.Instance.Pools.Get<DJKEntrancePoint>();
                point.Entrance1 = entrance;

                graph.AddPoint(point);
                CompleteEntrance(graph, point);
            }

            return graph;
        }

        private void CompleteEntrance(GraphController graph, DJKEntrancePoint currentPoint)
        {
            foreach (DJKEntrancePoint otherPoint in graph.Points)
            {
                if (otherPoint == currentPoint)
                    continue;

                if (otherPoint.Entrance1.entranceId != currentPoint.Entrance1.entranceId)
                    continue;

                otherPoint.Entrance2 = currentPoint.Entrance1;
                currentPoint.Entrance2 = otherPoint.Entrance1;

                Debug.Log($"adding neighbors to grah : from {currentPoint.Id} to {otherPoint.Id} pos {otherPoint.Entrance1.entrancePoint.position} dist {Const.DISTANCE_SAME_ENTRANCE}");
                graph.Neighbors[currentPoint.Id].Add(new DJKNeighbor(otherPoint.Id, otherPoint.Entrance1.entrancePoint.position, Const.DISTANCE_SAME_ENTRANCE));

                Debug.Log($"adding neighbors to grah : from {otherPoint.Id} to {currentPoint.Id} pos {currentPoint.Entrance1.entrancePoint.position} dist {Const.DISTANCE_SAME_ENTRANCE}");
                graph.Neighbors[otherPoint.Id].Add(new DJKNeighbor(currentPoint.Id, currentPoint.Entrance1.entrancePoint.position, Const.DISTANCE_SAME_ENTRANCE));

                return;
            }
        }

        private void CalculateNeighbors(GraphController graphToCalculate)
        {
            NeighborResult graphWriter = (from, to, startPos, targetPos, dist) =>
            {
                Debug.Log($"adding neighbors to grah : from {from} to {to} pos {targetPos} dist {dist}");
                graphToCalculate.Neighbors[from].Add(new DJKNeighbor(to, targetPos, dist));
                Debug.Log($"adding neighbors to grah : from {to} to {from} pos {startPos} dist {dist}");
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
