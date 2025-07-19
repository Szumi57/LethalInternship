using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest
{
    public class PointOfInterest : IPointOfInterest
    {
        private Dictionary<Type, IInterestPoint> interestPoints;

        private readonly List<Type> PriorityOrder = new List<Type>()
        {
            typeof(DefaultInterestPoint),
            typeof(VehicleInterestPoint),
            typeof(ShipInterestPoint)
        };

        public PointOfInterest()
        {
            interestPoints = new Dictionary<Type, IInterestPoint>();
        }

        public bool TryAddInterestPoint<T>(T interestPointToAdd) where T : IInterestPoint
        {
            // Dictionnary with Type as Key has to be populated by a method and the caller should infer the type
            // ex: TryAddInterestPoint<DefaultInterestPoint>(new DefaultInterestPoint(key)) not use IInterestPoint
            foreach (var existing in interestPoints.Values)
            {
                if (!interestPointToAdd.IsCompatibleWith(existing)
                    || !existing.IsCompatibleWith(interestPointToAdd))
                {
                    return false;
                }
            }

            if (interestPoints.ContainsKey(typeof(T)))
            {
                return false;
            }

            interestPoints[typeof(T)] = interestPointToAdd;
            return true;
        }

        public IEnumerable<IInterestPoint> GetInterestPoints()
        {
            return interestPoints.Values;
        }

        public EnumCommandTypes? GetCommand()
        {
            foreach (IInterestPoint interestPoint in interestPoints.Values)
            {
                if (interestPoint.CommandType == null)
                {
                    continue;
                }

                return interestPoint.CommandType;
            }

            return null;
        }

        public Vector3 GetPoint()
        {
            foreach (var type in PriorityOrder)
            {
                if (interestPoints.TryGetValue(type, out var interestPoint))
                {
                    return interestPoint.Point;
                }
            }

            foreach (IInterestPoint interestPoint in interestPoints.Values)
            {
                return interestPoint.Point;
            }

            return new Vector3();
        }
    }
}
