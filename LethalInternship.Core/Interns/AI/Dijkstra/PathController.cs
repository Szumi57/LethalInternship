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

        public void SetNewPath(List<IDJKPoint>? dJKPoints)
        {
            if (dJKPoints == null)
            {
                DJKPointsPath.Clear();
                IndexCurrentPoint = 0;
                return;
            }

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
                return string.Concat(pathString, $" empty, dest {destinationPoint.Id}");
            }
            else
            {
                float dist = 0;
                pathString = string.Empty;
                for (int i = 0; i < DJKPointsPath.Count; i++)
                {
                    IDJKPoint point = DJKPointsPath[i];

                    if (i < DJKPointsPath.Count - 1)
                    {
                        dist += point.Neighbors.First(x => x.neighbor.Id == DJKPointsPath[i + 1].Id).weight;
                    }

                    if (Array.IndexOf(DJKPointsPath.ToArray(), point) == IndexCurrentPoint)
                    {
                        pathString += $" >{point.Id}<";
                    }
                    else
                    {
                        pathString += $" {point.Id}";
                    }
                }

                return string.Concat($"Path ({(int)Mathf.Sqrt(dist)}m) = ", pathString);
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
