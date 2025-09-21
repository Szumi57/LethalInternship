using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathController
    {
        public List<IDJKPoint> DJKPointsPath { get; set; }
        public int IndexCurrentPoint { get; set; }

        private IDJKPoint? partialNextPoint;
        private bool currentPointReachable;

        private IDJKPoint sourcePoint;
        private IDJKPoint destinationPoint;

        public PathController()
        {
            DJKPointsPath = new List<IDJKPoint>();
            IndexCurrentPoint = 0;
        }

        public IDJKPoint GetCurrentPoint(bool getRealCurrentPoint = false)
        {
            if (DJKPointsPath.Count == 0)
            {
                return destinationPoint;
            }

            if (getRealCurrentPoint
                 || currentPointReachable)
            {
                if (DJKPointsPath.Count == 1)
                {
                    return DJKPointsPath[0];
                }
                return DJKPointsPath[IndexCurrentPoint];
            }
            else if (partialNextPoint != null)
            {
                return partialNextPoint;
            }

            return destinationPoint;
        }

        public Vector3 GetCurrentPoint(Vector3 currentPos, bool getRealCurrentPoint = false)
        {
            return GetCurrentPoint(getRealCurrentPoint).GetClosestPointFrom(currentPos);
        }

        public IDJKPoint GetSourcePoint()
        {
            return sourcePoint;

        }
        public IDJKPoint GetDestination()
        {
            return destinationPoint;
        }

        public void SetNewPath(List<IDJKPoint>? dJKPoints)
        {
            IndexCurrentPoint = 0;
            if (dJKPoints == null)
            {
                DJKPointsPath.Clear();
                return;
            }

            DJKPointsPath = dJKPoints;
            if (DJKPointsPath.Count > 1)
            {
                IndexCurrentPoint = 1;
            }
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

        public void SetNextPartialPoint(Vector3 nextPartialPoint)
        {
            if (DJKPointsPath == null || DJKPointsPath.Count == 0)
            {
                DJKPointsPath = new List<IDJKPoint>() { new DJKPositionPoint(0, nextPartialPoint) };
                IndexCurrentPoint = 0;
                currentPointReachable = false;
                return;
            }

            partialNextPoint = new DJKPositionPoint(DJKPointsPath[IndexCurrentPoint].Id, nextPartialPoint);
            currentPointReachable = false;
        }

        public void SetCurrentPointToReachable()
        {
            currentPointReachable = true;
        }

        public void SetToNextPoint()
        {
            IndexCurrentPoint = IndexCurrentPoint + 1 >= DJKPointsPath.Count ? IndexCurrentPoint : IndexCurrentPoint + 1;
        }

        public void SetNextPointToDestination()
        {
            IndexCurrentPoint = DJKPointsPath.Count - 1;
            currentPointReachable = true;
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
                return string.Concat(pathString, $" empty, dest {destinationPoint.Id}");
            }
            else
            {
                int dist = 0;
                pathString = string.Empty;
                for (int i = 0; i < DJKPointsPath.Count; i++)
                {
                    IDJKPoint point = DJKPointsPath[i];

                    if (i < DJKPointsPath.Count - 1)
                    {
                        dist += (int)Mathf.Sqrt(point.Neighbors.First(x => x.neighbor.Id == DJKPointsPath[i + 1].Id).weight);
                    }

                    if (Array.IndexOf(DJKPointsPath.ToArray(), point) == IndexCurrentPoint)
                    {
                        pathString += $" >{point.Id}{(currentPointReachable ? "" : "?")}<";
                    }
                    else
                    {
                        pathString += $" {point.Id}";
                    }
                }

                return string.Concat($"Path ({dist}m) = ", pathString);
            }
        }

        public string GetGraphString(List<IDJKPoint> graph)
        {
            string pathString = $"Graph({(graph == null ? 0 : graph.Count)})=";
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
                return string.Concat(pathString, string.Join("\r\n                                                               ", graph));
            }
        }
    }
}
