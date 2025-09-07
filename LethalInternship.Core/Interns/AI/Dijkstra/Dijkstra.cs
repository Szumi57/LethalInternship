using System.Collections.Generic;

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
                // Trouver le sommet non utilisé avec la plus petite distance
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] && (u == -1 || dist[i] < dist[u])) u = i;
                }
                if (u == -1 || dist[u] == INF) break;
                used[u] = true;

                // Relaxation des voisins
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

            // Reconstruction du chemin
            List<IDJKPoint> path = new List<IDJKPoint>();
            for (int at = dest.Id; at != -1; at = prev[at])
            {
                path.Add(points[at]);
            }
            path.Reverse();

            return path;
        }
    }
}
