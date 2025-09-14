using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

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

        public static IEnumerator CalculateNeighbors(CalculateNeighborsParameters parameters)
        {
            // Neighbors init
            NavMeshPath path = new NavMeshPath();

            HashSet<(int, int)> testedPairs = new HashSet<(int, int)>();
            foreach (var point1 in parameters.DJKPointsGraph)
            {
                foreach (var point2 in parameters.DJKPointsGraph)
                {
                    if (point1.Id == point2.Id
                        || point1.IsNeighborExist(point2)
                        || point2.IsNeighborExist(point1))
                    {
                        continue;
                    }

                    int idA = point1.Id, idB = point2.Id;
                    if (idA > idB) (idA, idB) = (idB, idA);
                    if (testedPairs.Contains((idA, idB)))
                    {
                        continue;
                    }

                    bool isNeighbors = false;
                    foreach (Vector3 point1point in point1.GetAllPoints())
                    {
                        if (isNeighbors)
                        {
                            break;
                        }

                        foreach (Vector3 point2point in point2.GetAllPoints())
                        {
                            yield return null;

                            //var timerCalculatePath = new Stopwatch();
                            //timerCalculatePath.Start();

                            NavMesh.CalculatePath(point1point, point2point, NavMesh.AllAreas, path);

                            //timerCalculatePath.Stop();
                            //PluginLoggerHook.LogDebug?.Invoke($"CalculatePath {point1.Id} - {point2.Id}{((this.path1.status == NavMeshPathStatus.PathComplete) ? "+" : "")} {timerCalculatePath.Elapsed.TotalMilliseconds}ms | {timerCalculatePath.Elapsed.ToString("mm':'ss':'fffffff")}");

                            testedPairs.Add((idA, idB));

                            if (path.status == NavMeshPathStatus.PathComplete)
                            {
                                float distance = GetFullDistancePath(path);
                                point1.TryAddToNeighbors(point2, distance);
                                point2.TryAddToNeighbors(point1, distance);

                                isNeighbors = true;
                                break;
                            }
                        }
                    }
                }
            }

            yield break;
        }

        private static float GetFullDistancePath(NavMeshPath path)
        {
            var corners = path.corners;
            float fullDistance = 0f;

            for (int i = 1; i < corners.Length; i++)
            {
                fullDistance += (corners[i - 1] - corners[i]).sqrMagnitude;
            }

            return fullDistance;
        }
    }
}
