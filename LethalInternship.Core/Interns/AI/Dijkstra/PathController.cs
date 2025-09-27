using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathController
    {
        public List<IDJKPoint> DJKPointsPath { get; set; }
        public int IndexCurrentPoint { get; set; }

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

            if (DJKPointsPath.Count == 1)
            {
                return DJKPointsPath[0];
            }

            return DJKPointsPath[IndexCurrentPoint];
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

        public void SetNewDestinationPositionPoint(Vector3 dest, string name)
        {
            SetNewDestination(new DJKPositionPoint(0, dest, name));
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

        public void SetCurrentPoint(Vector3 pos, string name = "")
        {
            if (DJKPointsPath == null || DJKPointsPath.Count == 0)
            {
                DJKPointsPath = new List<IDJKPoint>() { new DJKPositionPoint(0, pos) };
                IndexCurrentPoint = 0;
                return;
            }

            DJKPointsPath[IndexCurrentPoint] = new DJKPositionPoint(DJKPointsPath[IndexCurrentPoint].Id, pos, name);
        }

        public void SetToNextPoint()
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
                int dist = 0;
                pathString = string.Empty;
                for (int i = 0; i < DJKPointsPath.Count; i++)
                {
                    IDJKPoint point = DJKPointsPath[i];

                    // Calculate distance neighbor
                    if (i < DJKPointsPath.Count - 1)
                    {
                        foreach (var n in point.Neighbors)
                        {
                            if (n.neighbor.Id == DJKPointsPath[i + 1].Id)
                            {
                                dist += (int)Mathf.Sqrt(n.weight);
                                break;
                            }
                        }
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
