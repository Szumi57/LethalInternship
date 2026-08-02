using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathController
    {
        public readonly List<int> PathIds = new List<int>(32);
        public int IndexCurrentPoint { get; private set; }

        public void CopyFrom(PathController other)
        {
            PathIds.Clear();
            PathIds.AddRange(other.PathIds);
            IndexCurrentPoint = other.IndexCurrentPoint;
        }

        public void SetNewPath(List<int> pathIds)
        {
            Reset();
            PathIds.AddRange(pathIds);

            if (PathIds.Count > 1)
                IndexCurrentPoint = 1;
        }

        public int GetCurrentIdNodePath()
        {
            if (PathIds.Count == 1)
                return PathIds[0];

            return PathIds[IndexCurrentPoint];
        }

        public void Reset()
        {
            IndexCurrentPoint = 0;
            PathIds.Clear();
        }

        public void SetToNextPoint()
        {
            if (PathIds.Count == 0)
            {
                IndexCurrentPoint = 0;
                return;
            }
            IndexCurrentPoint = Math.Min(IndexCurrentPoint + 1, PathIds.Count - 1);
        }

        public void SetNextPointToDestination()
        {
            IndexCurrentPoint = Math.Max(PathIds.Count - 1, 0);
        }

        public bool IsCurrentPointDestination()
        {
            return PathIds.Count == 0 || IndexCurrentPoint == PathIds.Count - 1;
        }

        public bool IsPathValid()
        {
            return PathIds.Count >= 2;
        }
    }
}
