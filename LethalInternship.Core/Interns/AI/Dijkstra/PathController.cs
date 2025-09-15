using System.Collections.Generic;
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

        public void SetNewDestination(Vector3 source, Vector3 dest)
        {
            SetNewDestination(new DJKPositionPoint(0, source), new DJKPositionPoint(1, dest));
        }

        public void SetNewDestination(IDJKPoint source, IDJKPoint dest)
        {
            sourcePoint = source;
            destinationPoint = dest;

            //if (DJKPointsPath == null)
            //{
            //    DJKPointsPath = new List<IDJKPoint>() { source, dest };
            //    IndexCurrentPoint = 1;
            //}
            //else if (DJKPointsPath.Count == 0)
            //{
            //    DJKPointsPath.Add(source);
            //    DJKPointsPath.Add(dest);
            //    IndexCurrentPoint = 1;
            //}
            //else
            //{
            //    int i = DJKPointsPath.Count;
            //    source.Id = i++;
            //    dest.Id = i++;
            //    DJKPointsPath[0] = source;
            //    DJKPointsPath[DJKPointsPath.Count - 1] = dest;
            //    IndexCurrentPoint = IndexCurrentPoint == 0 ? 1 : IndexCurrentPoint;
            //}
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

        public bool IsCurrentPointDestination()
        {
            return IndexCurrentPoint == DJKPointsPath.Count - 1;
        }
    }
}
