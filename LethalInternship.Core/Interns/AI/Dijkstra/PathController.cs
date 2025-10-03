using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
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

        private IDJKPoint destinationPoint;

        public PathController()
        {
            DJKPointsPath = new List<IDJKPoint>();
            IndexCurrentPoint = 0;
        }

        public void ResetPathAndIndex()
        {
            IndexCurrentPoint = 0;
            DJKPointsPath.Clear();
        }

        public IDJKPoint GetCurrentPoint()
        {
            if (DJKPointsPath == null || DJKPointsPath.Count == 0)
            {
                return destinationPoint;
            }

            if (DJKPointsPath.Count == 1)
            {
                return DJKPointsPath[0];
            }

            return DJKPointsPath[IndexCurrentPoint];
        }

        public Vector3 GetCurrentPointPos(Vector3 actorPos)
        {
            if (DJKPointsPath == null || DJKPointsPath.Count == 0)
            {
                return destinationPoint.GetClosestPointTo(actorPos);
            }

            if (DJKPointsPath.Count == 1)
            {
                return DJKPointsPath[0].GetClosestPointTo(actorPos);
            }

            if (IndexCurrentPoint == 0)
            {
                return DJKPointsPath[0].GetClosestPointTo(actorPos);
            }
            else
            {
                Vector3 currentPointPos = DJKPointsPath[IndexCurrentPoint - 1].GetNeighborPos(DJKPointsPath[IndexCurrentPoint].Id);
                Vector3 trueCurrentPointPos = DJKPointsPath[IndexCurrentPoint].GetClosestPointTo(actorPos);
                if ((trueCurrentPointPos - currentPointPos).sqrMagnitude > 0.5f * 0.5f)
                {
                    return trueCurrentPointPos;
                }

                return currentPointPos;
            }
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

            DJKPointsPath = dJKPoints.Select(p => (IDJKPoint)p.Clone()).ToList();
            if (DJKPointsPath.Count > 1)
            {
                IndexCurrentPoint = 1;
            }
        }

        public void SetNewDestination(IDJKPoint dest)
        {
            destinationPoint = dest;

            if (DJKPointsPath.Count <= 0)
            {
                return;
            }

            if (dest.GetType() != DJKPointsPath[DJKPointsPath.Count - 1].GetType())
            {
                ResetPathAndIndex();
            }
        }

        public void SetCurrentPoint(IDJKPoint newCurrentPoint)
        {
            if (DJKPointsPath == null || DJKPointsPath.Count == 0)
            {
                DJKPointsPath = new List<IDJKPoint>() { newCurrentPoint };
                IndexCurrentPoint = 0;
                return;
            }

            newCurrentPoint.Id = DJKPointsPath[IndexCurrentPoint].Id;
            newCurrentPoint.SetNeighbors(DJKPointsPath[IndexCurrentPoint].Neighbors
                                                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                                                .ToList());
            DJKPointsPath[IndexCurrentPoint] = newCurrentPoint;
        }

        public void SetToNextPoint()
        {
            if (IndexCurrentPoint == DJKPointsPath.Count - 1)
            {
                ResetPathAndIndex();
                return;
            }

            IndexCurrentPoint = IndexCurrentPoint + 1;
        }

        public void SetNextPointToDestination()
        {
            IndexCurrentPoint = DJKPointsPath.Count - 1;
        }

        public bool IsCurrentPointDestination()
        {
            return IndexCurrentPoint == DJKPointsPath.Count - 1 && GetCurrentPoint() == destinationPoint;
        }

        public bool IsPathNotValid()
        {
            return DJKPointsPath == null || DJKPointsPath.Count < 2;
        }

        public string GetFullPathString()
        {
            string pathString = $"Path = ";
            if (DJKPointsPath == null)
            {
                return string.Concat(pathString, " null");
            }
            else if (DJKPointsPath.Count == 0)
            {
                return string.Concat(pathString, $" empty, dest {destinationPoint}");
            }
            else
            {
                pathString = string.Empty;
                for (int i = 0; i < DJKPointsPath.Count; i++)
                {
                    IDJKPoint point = DJKPointsPath[i];
                    if (i < DJKPointsPath.Count - 1)
                    {
                        pathString += $"{point.Id} => {point.GetNeighborPos(DJKPointsPath[i + 1].Id)}";
                    }
                    else
                    {
                        pathString += $"{point.Id}";
                    }
                }

                return string.Concat($"Path = ", pathString);
            }
        }

        public override string ToString()
        {
            string pathString = $"Path = ";
            if (DJKPointsPath == null)
            {
                return string.Concat(pathString, " null");
            }
            else if (DJKPointsPath.Count == 0)
            {
                return string.Concat(pathString, $" empty, dest {destinationPoint}");
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
                            if (n.idNeighbor == DJKPointsPath[i + 1].Id)
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
    }
}
