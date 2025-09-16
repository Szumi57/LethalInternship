using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathController
    {
        public List<IDJKPoint> DJKPointsPath { get; set; }
        public int IndexCurrentPoint { get; set; }

        private IDJKPoint sourcePoint { get; set; }
        private IDJKPoint destinationPoint { get; set; }

        public PathController()
        {
            DJKPointsPath = new List<IDJKPoint>();
            IndexCurrentPoint = 0;
        }

        public IDJKPoint GetCurrentPoint()
        {
            if (DJKPointsPath.Count == 0)
            {
                return destinationPoint;
            }

            if (DJKPointsPath.Count == 1)
            {
                return DJKPointsPath[0];
            }

            return DJKPointsPath[IndexCurrentPoint];
        }

        public Vector3 GetCurrentPoint(Vector3 currentPos)
        {
            return GetCurrentPoint().GetClosestPointFrom(currentPos);
        }

        public IDJKPoint GetSourcePoint()
        {
            return sourcePoint;

        }
        public IDJKPoint GetDestination()
        {
            return destinationPoint;
        }

        public void SetNewPath(List<IDJKPoint> dJKPoints)
        {
            DJKPointsPath = dJKPoints;
            IndexCurrentPoint = 1;
        }

        public void SetNewDestination(Vector3 dest)
        {
            SetNewDestination(new DJKPositionPoint(0, dest));
        }

        public void SetNewDestination(IDJKPoint dest)
        {
            destinationPoint = dest;
            if (DJKPointsPath.Count > 0)
            {
                destinationPoint.Id = DJKPointsPath[DJKPointsPath.Count - 1].Id;
                DJKPointsPath[DJKPointsPath.Count - 1] = destinationPoint;
            }
        }

        public void SetNextPoint(int index)
        {
            IndexCurrentPoint = index;
        }

        public void SetNextPoint()
        {
            IndexCurrentPoint = IndexCurrentPoint + 1 >= DJKPointsPath.Count ? IndexCurrentPoint : IndexCurrentPoint + 1;
        }

        public void SetNextPointToDestination()
        {
            IndexCurrentPoint = DJKPointsPath.Count - 1;
        }

        public bool IsCurrentPointDestination()
        {
            return IndexCurrentPoint == DJKPointsPath.Count - 1;
        }

        public string GetPathString()
        {
            string pathString = $"Path = ";
            if (DJKPointsPath == null)
            {
                return string.Concat(pathString, " null");
            }
            else if (DJKPointsPath.Count == 0)
            {
                return string.Concat(pathString, " empty");
            }
            else
            {
                return string.Concat(pathString, string.Join(" ", DJKPointsPath.Select(x => x.Id)));
            }
        }

        public string GetGraphString(List<IDJKPoint> graph)
        {
            string pathString = $"Graph({(graph == null ? 0 : graph.Count)}) = \r\n";
            if (graph == null)
            {
                return string.Concat(pathString, " null");
            }
            else if (graph.Count == 0)
            {
                return string.Concat(pathString, " empty");
            }
            else
            {
                return string.Concat(pathString, string.Join("\r\n                                                              ", graph));
            }
        }
    }
}
