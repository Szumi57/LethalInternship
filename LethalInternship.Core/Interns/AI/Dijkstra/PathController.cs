using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class PathController
    {
        public List<IDJKPoint> DJKPointsPath { get; set; }
        public int IndexCurrentPoint { get; set; }

        public PathController()
        {
            DJKPointsPath = new List<IDJKPoint>();
            IndexCurrentPoint = 0;
        }

        public Vector3 GetCurrentPoint(Vector3 currentPos)
        {
            return DJKPointsPath[IndexCurrentPoint].GetClosestPointFrom(currentPos);
        }

        public Vector3 GetSourcePoint(Vector3 currentPos)
        {
            return DJKPointsPath[0].GetClosestPointFrom(currentPos);

        }
        public Vector3 GetDestination(Vector3 currentPos)
        {
            return DJKPointsPath[DJKPointsPath.Count - 1].GetClosestPointFrom(currentPos);
        }

        public void SetNewPath(List<IDJKPoint> dJKPoints)
        {
            DJKPointsPath = dJKPoints;
            IndexCurrentPoint = 1;
        }

        public void SetNewDestination(Vector3 source, Vector3 dest)
        {
            SetNewDestination(new DJKPositionPoint(0, source), new DJKPositionPoint(1, dest));
        }

        public void SetNewDestination(IDJKPoint source, IDJKPoint dest)
        {
            if (DJKPointsPath == null)
            {
                DJKPointsPath = new List<IDJKPoint>() { source, dest };
            }
            else
            {
                int i = DJKPointsPath.Count;
                source.Id = i++;
                dest.Id = i++;
                DJKPointsPath[0] = source;
                DJKPointsPath[DJKPointsPath.Count - 1] = dest;
            }

            IndexCurrentPoint = 1;
        }

        public void SetNextPoint(int index)
        {
            IndexCurrentPoint = index;
        }

        public void SetNextPoint()
        {
            IndexCurrentPoint++;
        }

        public void SetNextPointToDestination()
        {
            IndexCurrentPoint = DJKPointsPath.Count - 1;
        }
    }
}
