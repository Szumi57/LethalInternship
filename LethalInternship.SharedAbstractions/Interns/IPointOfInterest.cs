using LethalInternship.SharedAbstractions.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IPointOfInterest
    {
        Vector3 GetPoint();
        bool TryAddInterestPoint<T>(T interestPointToAdd) where T : IInterestPoint;
        IEnumerable<IInterestPoint> GetInterestPoints();
        EnumCommandTypes? GetCommand();
    }
}
