using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathfindingContext : IDJKNodeSource
    {
        private readonly StringBuilder _pathSb = new StringBuilder(512);

        public GraphController SharedGraph = null!;

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
            SharedGraph = other.SharedGraph;
            SetStart(other.Start.Clone(InternManager.Instance.Pools));
            SetDestination(other.destination.Clone(InternManager.Instance.Pools));

            StartNeighbors.Clear();
            StartNeighbors.AddRange(other.StartNeighbors);

            DestinationNeighbors.Clear();
            DestinationNeighbors.AddRange(other.DestinationNeighbors);
        }

        public void Clear(bool clearDest = true)
        {
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
            int entranceCount = SharedGraph.Points.Count;

            // Start (id 0)
            if (nodeId == 0)
            {
                foreach (var n in StartNeighbors)
                    yield return n;
                yield break;
            }

            // Destination (id N+1)
            if (nodeId == entranceCount + 1)
            {
                foreach (var n in DestinationNeighbors)
                    yield return n;
                yield break;
            }

            // Entrance (id 1..N)
            int entranceId = nodeId - 1;

            // neighbors entrance ↔ entrance
            foreach (var n in SharedGraph.Neighbors[entranceId])
            {
                yield return new DJKNeighbor(
                    n.ToId + 1,
                    n.Pos,
                    n.Cost);
            }

            // entrance → destination
            foreach (var n in DestinationNeighbors)
            {
                if (n.ToId == entranceId)
                {
                    yield return new DJKNeighbor(
                        entranceCount + 1,
                        n.Pos,
                        n.Cost);
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
            int entranceId = nodeId - 1;
            return SharedGraph.Points[entranceId];
        }

        public Vector3 GetCurrentTargetPos(int index,
                                           IReadOnlyList<int> pathIds,
                                           Vector3 actorPos,
                                           IDJKPoint destination)
        {
            int pathCount = pathIds.Count;

            // No path, go directly to destination
            if (pathCount == 0)
                return destination.GetClosestPointTo(actorPos);

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
            else if (toId == Destination.Id)
            {
                foreach (var n in DestinationNeighbors)
                {
                    if (n.ToId == fromId)// inverted
                    {
                        neighbor = n;
                        return true;
                    }
                }
            }
            // Entrance
            else
            {
                int entranceId = fromId;
                foreach (var n in SharedGraph.Neighbors[entranceId])
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
            int entranceId = nodeId - 1;
            return SharedGraph.Points[entranceId].GetClosestPointTo(fromPos);
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
                _pathSb.Append(fromId);

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

        public string ToString(int indexCurrentPoint, IReadOnlyList<int> pathIds)
        {
            _pathSb.Clear();
            _pathSb.Append("Path (");
            _pathSb.Append((int)Mathf.Sqrt(GetFullPathDistance(pathIds)));
            _pathSb.Append("m) = ");

            if (pathIds == null || pathIds.Count == 0)
            {
                _pathSb.Append("Path : empty");
                return _pathSb.ToString();
            }

            for (int i = 0; i < pathIds.Count; i++)
            {
                int fromId = pathIds[i];
                if (i == indexCurrentPoint)
                {
                    _pathSb.Append(" >");
                    _pathSb.Append(fromId);
                    _pathSb.Append("<");
                }
                else
                {
                    _pathSb.Append(" ");
                    _pathSb.Append(fromId);
                }
            }

            return _pathSb.ToString();
        }
    }
}
