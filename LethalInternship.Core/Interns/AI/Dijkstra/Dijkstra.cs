using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class Dijkstra
    {
        const float INF = float.MaxValue / 4;

        public static List<IDJKPoint> CalculatePath(List<IDJKPoint> points, IDJKPoint src, IDJKPoint dest)
        {
            int n = points.Count;
            double[] dist = new double[n];
            bool[] used = new bool[n];
            int[] prev = new int[n];

            for (int i = 0; i < n; i++)
            {
                dist[i] = INF;
                used[i] = false;
                prev[i] = -1;
            }
            dist[src.Id] = 0;

            for (int k = 0; k < n; k++)
            {
                // Find the point unused with smallest distance
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] && (u == -1 || dist[i] < dist[u])) u = i;
                }
                if (u == -1 || dist[u] == INF) break;
                used[u] = true;

                // Check neighbors
                foreach (var (neighbor, weight) in points[u].Neighbors)
                {
                    int v = neighbor.Id;
                    if (!used[v] && dist[u] + weight < dist[v])
                    {
                        dist[v] = dist[u] + weight;
                        prev[v] = u;
                    }
                }
            }

            // Reconstruct path
            List<IDJKPoint> path = new List<IDJKPoint>();
            for (int at = dest.Id; at != -1; at = prev[at])
            {
                path.Add(points[at]);
            }
            path.Reverse();

            return path;
        }

        public static List<InstructionParameters> GenerateWorkCalculateNeighbors(List<IDJKPoint> DJKPointsGraph)
        {
            // Neighbors init
            var instructions = new List<InstructionParameters>();

            HashSet<(int, int)> testedPairs = new HashSet<(int, int)>();
            foreach (var point1 in DJKPointsGraph)
            {
                foreach (var point2 in DJKPointsGraph)
                {
                    if (point1.Id == point2.Id)
                    {
                        continue;
                    }

                    int idA = point1.Id, idB = point2.Id;
                    if (idA > idB) (idA, idB) = (idB, idA);
                    if (testedPairs.Contains((idA, idB)))
                    {
                        continue;
                    }
                    testedPairs.Add((idA, idB));

                    // Add neighbors if exists
                    float? distanceNeighbor = point1.GetNeighborDistanceIfExist(point2);
                    if (distanceNeighbor != null)
                    {
                        point2.TryAddToNeighbors(point1, distanceNeighbor.Value);
                        continue;
                    }
                    distanceNeighbor = point2.GetNeighborDistanceIfExist(point1);
                    if (distanceNeighbor != null)
                    {
                        point1.TryAddToNeighbors(point2, distanceNeighbor.Value);
                        continue;
                    }

                    int groupId = InternManager.Instance.GetNewInstructionGroupId();
                    foreach (Vector3 point1point in point1.GetAllPoints())
                    {
                        foreach (Vector3 point2point in point2.GetNearbyPoints(point1point))
                        {
                            // Ask for calculate path
                            instructions.Add(new InstructionParameters(
                                groupId,
                                start: point1point,
                                target: point2point,
                                startDJKPoint: point1,
                                targetDJKPoint: point2
                            ));
                        }
                    }
                }
            }

            return instructions;
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
            return dist + (lastCorner - target).sqrMagnitude * 1000f;
        }
    }
}
