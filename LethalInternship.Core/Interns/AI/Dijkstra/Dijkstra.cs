using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class Dijkstra
    {
        const float INF = float.MaxValue / 4;

        static double[] dist = Array.Empty<double>();
        static bool[] used = Array.Empty<bool>();
        static int[] prev = Array.Empty<int>();

        public static void CalculatePath(IDJKNodeSource source,
                                         int src,
                                         int dest,
                                         List<int> resultPath)
        {
            int n = source.NodeCount;
            EnsureCapacity(n);

            for (int i = 0; i < n; i++)
            {
                dist[i] = INF;
                used[i] = false;
                prev[i] = -1;
            }

            dist[src] = 0;

            for (int k = 0; k < n; k++)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] && (u == -1 || dist[i] < dist[u]))
                        u = i;
                }

                if (u == -1 || dist[u] == INF)
                    break;

                used[u] = true;

                foreach (var neighbor in source.GetNeighbors(u))
                {
                    Debug.Log($"Check neighbor of {u} ToId {neighbor.ToId} Cost {neighbor.Cost}");
                    int v = neighbor.ToId;
                    double w = neighbor.Cost;

                    if (!used[v] && dist[u] + w < dist[v])
                    {
                        Debug.Log($"used[v]={used[v]} dist[u]={dist[u]} w={w} dist[v]={dist[v]}");
                        dist[v] = dist[u] + w;
                        prev[v] = u;
                    }
                }
            }

            // reconstruction
            resultPath.Clear();

            for (int at = dest; at != -1; at = prev[at])
            {
                Debug.Log($"at {at}");
                resultPath.Add(at);
            }

            resultPath.Reverse();
        }

        private static void EnsureCapacity(int n)
        {
            if (dist.Length < n)
            {
                dist = new double[n];
                used = new bool[n];
                prev = new int[n];
            }
        }

        public static void GenerateNeighborInstructions(List<IDJKPoint> DJKPointsGraph,
                                                        int idBatch,
                                                        NeighborResult graphWriter,
                                                        List<IInstruction> output)
        {
            // Neighbors init
            output.Clear();
            for (int i = 0; i < DJKPointsGraph.Count; i++)
            {
                var point1 = DJKPointsGraph[i];
                for (int j = i + 1; j < DJKPointsGraph.Count; j++)
                {
                    var point2 = DJKPointsGraph[j];

                    int groupId = InternManager.Instance.GetNewInstructionGroupId();
                    foreach (Vector3 point1point in point1.GetAllPoints())
                    {
                        foreach (Vector3 point2point in point2.GetNearbyPoints(point1point))
                        {
                            // Ask for calculate path
                            var parameters = new InstructionParameters(
                                groupId,
                                start: point1point,
                                target: point2point,
                                startDJKPoint: point1,
                                targetDJKPoint: point2,
                                resultCallback: graphWriter
                            );

                            output.Add(point2.GenerateInstruction(idBatch, parameters));
                        }
                    }
                }
            }
        }

        public static void GenerateNeighborInstructions(PathfindingContext pf,
                                                        int idBatch,
                                                        NeighborResult startWriter,
                                                        NeighborResult destinationWriter,
                                                        List<IInstruction> output)
        {
            output.Clear();

            GenerateInstructionsFromStart(
                pf.Start,
                pf.SharedGraph.Points,
                idBatch,
                startWriter,
                output);

            GenerateInstructionsToDest(
                pf.Destination,
                pf.SharedGraph.Points,
                idBatch,
                destinationWriter,
                output);

            GenerateInstructionsFromStartToDest(
                pf.Start,
                pf.Destination,
                idBatch,
                startWriter,
                output);
        }

        private static void GenerateInstructionsFromStartToDest(IDJKPoint start,
                                                                IDJKPoint dest,
                                                                int idBatch,
                                                                NeighborResult startWriter,
                                                                List<IInstruction> output)
        {
            int groupId = InternManager.Instance.GetNewInstructionGroupId();

            foreach (var p1 in start.GetAllPoints())
            {
                foreach (var p2 in dest.GetNearbyPoints(p1))
                {
                    var parameters = new InstructionParameters(
                            groupId,
                            p1,
                            p2,
                            start,
                            dest,
                            startWriter);

                    output.Add(dest.GenerateInstruction(idBatch, parameters));
                }
            }
        }

        private static void GenerateInstructionsFromStart(IDJKPoint start,
                                                         List<IDJKPoint> graphPoints,
                                                         int idBatch,
                                                         NeighborResult startWriter,
                                                         List<IInstruction> output)
        {
            foreach (var entrance in graphPoints)
            {
                int groupId = InternManager.Instance.GetNewInstructionGroupId();

                foreach (var p1 in start.GetAllPoints())
                {
                    foreach (var p2 in entrance.GetNearbyPoints(p1))
                    {
                        var parameters = new InstructionParameters(
                                groupId,
                                p1,
                                p2,
                                start,
                                entrance,
                                startWriter);

                        output.Add(entrance.GenerateInstruction(idBatch, parameters));
                    }
                }
            }
        }

        private static void GenerateInstructionsToDest(IDJKPoint dest,
                                                       List<IDJKPoint> graphPoints,
                                                       int idBatch,
                                                       NeighborResult destinationWriter,
                                                       List<IInstruction> output)
        {
            foreach (var entrance in graphPoints)
            {
                int groupId = InternManager.Instance.GetNewInstructionGroupId();

                foreach (var p1 in entrance.GetAllPoints())
                {
                    foreach (var p2 in dest.GetNearbyPoints(p1))
                    {
                        var parameters = new InstructionParameters(
                                groupId,
                                p1,
                                p2,
                                entrance,
                                dest,
                                destinationWriter
                                );

                        output.Add(dest.GenerateInstruction(idBatch, parameters));
                    }
                }
            }
        }

        public static float GetFullDistancePath(Vector3[] corners)
        {
            float fullDistance = 0f;
            for (int i = 1; i < corners.Length; i++)
            {
                fullDistance += (corners[i - 1] - corners[i]).sqrMagnitude;
            }

            return fullDistance < 1f ? 1f : fullDistance;
        }

        public static float ApplyPartialPathPenalty(float dist, Vector3 lastCorner, Vector3 target)
        {
            return dist + (lastCorner - target).sqrMagnitude * 10000f;
        }

        public static Vector3 GetSampledPos(Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out NavMeshHit hitEnd, 2f, NavMesh.AllAreas))
            {
                float diff = (pos - hitEnd.position).sqrMagnitude;
                if (diff > 0.1f * 0.1f)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"Using pos sampled position, diff dist {Mathf.Sqrt(diff)}");
                }
                pos = hitEnd.position;
            }

            return pos;
        }
    }
}
