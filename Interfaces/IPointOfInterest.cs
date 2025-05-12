using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IPointOfInterest
    {
        Vector3 GetPoint();
        bool TryAddInterestPoint<T>(T interestPointToAdd) where T : IInterestPoint;
        IEnumerable<IInterestPoint> GetInterestPoints();
    }
}
