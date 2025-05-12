using System;
using System.Collections.Generic;
using LethalInternship.Interfaces;
using UnityEngine;

namespace LethalInternship.Interns.AI.PointsOfInterest
{
    public class PointOfInterest : IPointOfInterest
    {
        private Vector3 point;
        private Dictionary<Type, IInterestPoint> interestPoints;

        public PointOfInterest(IInterestPoint interestPoint)
        {
            point = interestPoint.Point;

            interestPoints = new Dictionary<Type, IInterestPoint>();
            TryAddInterestPoint(interestPoint);
        }

        public Vector3 GetPoint()
        {
            return point;
        }

        public bool TryAddInterestPoint<T>(T interestPointToAdd) where T : IInterestPoint
        {
            foreach (var existing in interestPoints.Values)
            {
                if (!interestPointToAdd.IsCompatibleWith(existing)
                    || !existing.IsCompatibleWith(interestPointToAdd))
                {
                    return false;
                }
            }

            interestPoints[typeof(T)] = interestPointToAdd;
            return true;
        }

        public IEnumerable<IInterestPoint> GetInterestPoints()
        {
            return interestPoints.Values;
        }
    }
}
