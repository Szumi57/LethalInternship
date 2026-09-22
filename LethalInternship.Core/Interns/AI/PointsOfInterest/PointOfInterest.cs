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

        private readonly List<Type> priorityOrder = new List<Type>()
        {
            typeof(PositionInterestPoint),
            typeof(VehicleInterestPoint),
            typeof(ShipInterestPoint),
            typeof(GatheringInterestPoint)
        };

        public bool IsInvalid
        {
            get
            {
                if (interestPoints.Count == 0)
                    return true;

                foreach (IInterestPoint interestPoint in GetListInterestPoints())
                {
                    if (interestPoint.IsInvalid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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

            // Careful when adding an interface type of concrete class in a key of dictionnary
            // Always take the real type with .GetType() not typeof()
            // When an interface is passed in parameter and not a concrete type
            Type ipType = interestPointToAdd.GetType();
            if (interestPoints.ContainsKey(ipType))
            {
                return false;
            }

            interestPoints[ipType] = interestPointToAdd;
            return true;
        }

        public bool TryRemoveInterestPointType(Type interestPointTypeToRemove)
        {
            // Always take the real type with .GetType() not typeof()
            // When an interface is passed in parameter and not a concrete type
            return interestPoints.Remove(interestPointTypeToRemove);
        }

        public IEnumerable<IInterestPoint> GetListInterestPoints()
        {
            return interestPoints.Values;
        }

        public Dictionary<Type, IInterestPoint> GetDictTypeInterestPoints()
        {
            return interestPoints;
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

        public IInterestPoint? GetInterestPoint()
        {
            foreach (var type in priorityOrder)
            {
                if (interestPoints.TryGetValue(type, out var interestPoint))
                {
                    return interestPoint;
                }
            }

            foreach (IInterestPoint interestPoint in interestPoints.Values)
            {
                return interestPoint;
            }

            return null;
        }

        public Vector3 GetPoint()
        {
            IInterestPoint? interestPoint = GetInterestPoint();
            return interestPoint == null ? new Vector3() : interestPoint.Point;
        }
    }
}
