using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class GraphController
    {
        private const int NB_POINTS_BEFORE_GRAPH = 1;

        private static readonly StringBuilder _sb = new StringBuilder(512);

        public List<IDJKPoint> Points { get; } = new List<IDJKPoint>();

        public List<DJKNeighbor>[] Neighbors { get; private set; } = null!;

        public GraphController() { }

        public GraphController(int capacity)
        {
            Init(capacity);
        }

        public void Init(int capacity)
        {
            if (Neighbors == null || Neighbors.Length != capacity)
                Neighbors = new List<DJKNeighbor>[capacity];

            for (int i = 0; i < capacity; i++)
                Neighbors[i] = new List<DJKNeighbor>(8);
        }

        public void Reset()
        {
            foreach (IDJKPoint point in Points)
            {
                point.ReturnToPool(InternManager.Instance.Pools);
            }
            Points.Clear();

            if (Neighbors != null)
            {
                foreach (var neighbors in Neighbors)
                {
                    neighbors.Clear();
                }
            }
        }

        public void CopyFrom(GraphController other)
        {
            Init(other.Neighbors.Length);
            foreach (var point in other.Points)
            {
                this.Points.Add(point.Clone(InternManager.Instance.Pools));
            }

            for (int i = 0; i < other.Neighbors.Length; i++)
            {
                Neighbors[i].Clear();
                Neighbors[i].AddRange(other.Neighbors[i]);
            }
        }

        public void AddPoint(IDJKPoint point)
        {
            point.Id = Points.Count + NB_POINTS_BEFORE_GRAPH;
            Points.Add(point);
        }

        public IDJKPoint GetPoint(int index)
        {
            return Points[index - NB_POINTS_BEFORE_GRAPH];
        }

        public override string ToString()
        {
            _sb.Clear();

            for (int i = 0; i < Points.Count; i++)
            {
                _sb.Append("  ");
                _sb.Append(Points[i]);

                var neighbors = Neighbors[i + NB_POINTS_BEFORE_GRAPH];
                if (neighbors.Count > 0)
                {
                    _sb.Append(" -> [");
                    for (int n = 0; n < neighbors.Count; n++)
                    {
                        var neigh = neighbors[n];
                        _sb.Append(neigh.ToId);
                        _sb.Append('(');
                        _sb.Append((int)Mathf.Sqrt(neigh.Cost));
                        _sb.Append(')');
                        if (n < neighbors.Count - 1) _sb.Append(", ");
                    }
                    _sb.Append(']');
                }

                _sb.AppendLine();
            }

            return _sb.ToString();
        }
    }
}
