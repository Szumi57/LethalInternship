using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathfindingContext : IDJKNodeSource
    {
        private readonly StringBuilder _pathSb = new StringBuilder(512);
        private readonly StringBuilder _sb = new StringBuilder(512);

        public GraphController SharedGraph = new GraphController(Const.GRAPH_CAPACITY);

        public IDJKPoint Start { get; private set; } = null!;
        private IDJKPoint destination = null!;
        public IDJKPoint Destination
        {
            get
            {
                if (destination != null)
                    destination.Id = NodeCount - 1;

                return destination!;
            }
        }

        public readonly List<DJKNeighbor> StartNeighbors = new List<DJKNeighbor>(32);
        public readonly List<DJKNeighbor> DestinationNeighbors = new List<DJKNeighbor>(32);

        public int NodeCount => 2 + (SharedGraph == null ? 0 : SharedGraph.Points.Count);

        public void CopyFrom(PathfindingContext other)
        {
            SharedGraph.Reset();
            SharedGraph.CopyFrom(other.SharedGraph);

            SetStart(other.Start.Clone(InternManager.Instance.Pools));
            SetDestination(other.destination.Clone(InternManager.Instance.Pools));

            StartNeighbors.Clear();
            StartNeighbors.AddRange(other.StartNeighbors);

            DestinationNeighbors.Clear();
            DestinationNeighbors.AddRange(other.DestinationNeighbors);
        }

        public void Clear(bool clearDest = true)
        {
            SharedGraph.Reset();
            StartNeighbors.Clear();
            DestinationNeighbors.Clear();

            if (Start != null)
            {
                Start.ReturnToPool(InternManager.Instance.Pools);
                Start = null!;
            }
            if (clearDest && destination != null)
            {
                destination.ReturnToPool(InternManager.Instance.Pools);
                destination = null!;
            }
        }

        public void SetStart(IDJKPoint start)
        {
            Start?.ReturnToPool(InternManager.Instance.Pools);
            Start = start;
            start.Id = 0;
        }
        public void SetDestination(IDJKPoint dest)
        {
            destination?.ReturnToPool(InternManager.Instance.Pools);
            destination = dest;
            dest.Id = NodeCount - 1;
        }

        public IEnumerable<DJKNeighbor> GetNeighbors(int nodeId)
        {
            // Start (id 0)
            if (nodeId == 0)
            {
                foreach (var n in StartNeighbors)
                    yield return n;
                yield break;
            }

            // Destination (id N+1)
            if (nodeId == destination.Id)
            {
                foreach (var n in DestinationNeighbors)
                    yield return n;
                yield break;
            }

            // neighbors entrance <> entrance
            foreach (var n in SharedGraph.Neighbors[nodeId])
            {
                yield return n;
            }

            // entrance > destination
            foreach (var n in DestinationNeighbors)
            {
                if (n.ToId == nodeId)
                {
                    yield return n;
                }
            }
        }

        public IDJKPoint GetCurrentTargetPoint(int nodeId)
        {
            // Start
            if (nodeId == 0)
                return Start;

            // Destination
            if (nodeId == Destination.Id)
                return Destination;

            // Entrance
            return SharedGraph.GetPoint(nodeId);
        }

        public Vector3 GetCurrentTargetPos(int index,
                                           IReadOnlyList<int> pathIds,
                                           Vector3 actorPos)
        {
            int pathCount = pathIds.Count;

            // No path, go directly to destination
            if (pathCount == 0)
                return Destination.GetClosestPointTo(actorPos);

            // First node
            if (index == 0)
            {
                int firstId = pathIds[0];
                return GetClosestPointToNode(firstId, actorPos);
            }

            int fromId = pathIds[index - 1];
            int toId = pathIds[index];

            DJKNeighbor neighbor;
            if (!TryGetNeighbor(fromId, toId, out neighbor))
            {
                // Fallback safety
                return GetClosestPointToNode(toId, actorPos);
            }

            Vector3 currentPos = neighbor.Pos;
            Vector3 projected = GetClosestPointToNode(toId, currentPos);

            if ((currentPos - projected).sqrMagnitude > 1f)
                return projected;

            return currentPos;
        }

        public bool TryGetNeighbor(int fromId, int toId, out DJKNeighbor neighbor)
        {
            // Start
            if (fromId == 0)
            {
                foreach (var n in StartNeighbors)
                {
                    if (n.ToId == toId)
                    {
                        neighbor = n;
                        return true;
                    }
                }
            }
            // Destination neighbors are inverted
            //else if (toId == Destination.Id)
            //{
            //    foreach (var n in DestinationNeighbors)
            //    {
            //        if (n.ToId == fromId)// inverted
            //        {
            //            neighbor = n;
            //            return true;
            //        }
            //    }
            //}
            // Entrance
            else
            {
                foreach (var n in SharedGraph.Neighbors[fromId])
                {
                    if (n.ToId == toId)
                    {
                        neighbor = n;
                        return true;
                    }
                }
            }

            neighbor = default;
            return false;
        }

        private Vector3 GetClosestPointToNode(int nodeId, Vector3 fromPos)
        {
            // Start
            if (nodeId == 0)
                return fromPos;

            // Destination
            if (nodeId == Destination.Id)
                return Destination.GetClosestPointTo(fromPos);

            // Entrance
            return SharedGraph.GetPoint(nodeId).GetClosestPointTo(fromPos);
        }

        public float GetFullPathDistance(IReadOnlyList<int> pathIds)
        {
            if (pathIds == null || pathIds.Count < 2)
                return float.MaxValue;

            float total = 0f;

            for (int i = 0; i < pathIds.Count - 1; i++)
            {
                int fromId = pathIds[i];
                int toId = pathIds[i + 1];

                if (!TryGetNeighbor(fromId, toId, out var neighbor))
                {
                    // no path / graph invalid
                    return float.MaxValue;
                }

                total += neighbor.Cost;
            }

            return total;
        }

        public string GetFullPathString(IReadOnlyList<int> pathIds)
        {
            _pathSb.Clear();
            _pathSb.Append("Path = ");

            if (pathIds == null || pathIds.Count == 0)
            {
                _pathSb.Append("empty");
                return _pathSb.ToString();
            }

            for (int i = 0; i < pathIds.Count; i++)
            {
                int fromId = pathIds[i];
                _pathSb.Append("[");
                _pathSb.Append(fromId);
                _pathSb.Append("]");

                if (i < pathIds.Count - 1)
                {
                    int toId = pathIds[i + 1];

                    if (TryGetNeighbor(fromId, toId, out var neighbor))
                    {
                        _pathSb.Append(" => ");
                        _pathSb.Append(neighbor.Pos);
                    }
                    else
                    {
                        _pathSb.Append(" => ?");
                    }
                }
            }

            return _pathSb.ToString();
        }

        public override string ToString()
        {
            _sb.Clear();

            _sb.AppendLine();
            _sb.Append("  Start: ");
            _sb.Append(Start?.ToString() ?? "null");
            _sb.Append(" -> [");
            AppendNeighbors(_sb, StartNeighbors);
            _sb.AppendLine("]");

            _sb.Append(SharedGraph);

            _sb.Append("  Destination: ");
            _sb.Append(destination?.ToString() ?? "null");
            _sb.Append(" -> [");
            AppendNeighbors(_sb, DestinationNeighbors);
            _sb.AppendLine("]");

            return _sb.ToString();
        }

        private static void AppendNeighbors(StringBuilder sb,
                                            IReadOnlyList<DJKNeighbor> neighbors)
        {
            for (int i = 0; i < neighbors.Count; i++)
            {
                var neighbor = neighbors[i];

                sb.Append(neighbor.ToId);
                sb.Append('(');
                sb.Append((int)Mathf.Sqrt(neighbor.Cost));
                sb.Append(')');

                if (i < neighbors.Count - 1)
                    sb.Append(", ");
            }
        }
    }
}
