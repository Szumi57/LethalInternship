using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IPointOfInterest
    {
        bool IsInvalid { get; }
        IInterestPoint? GetInterestPoint();
        Vector3 GetPoint();
        bool TryAddInterestPoint<T>(T interestPointToAdd) where T : IInterestPoint;
        IEnumerable<IInterestPoint> GetListInterestPoints();
        Dictionary<Type, IInterestPoint> GetDictTypeInterestPoints();
        EnumCommandTypes? GetCommand();
    }
}
