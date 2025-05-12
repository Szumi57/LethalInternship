using LethalInternship.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Interns.AI.PointsOfInterest.InterestPoints
{
    public abstract class InterestPointBase : IInterestPoint
    {
        public abstract Vector3 Point { get; }

        protected virtual IEnumerable<Type> IncompatibleTypes => Enumerable.Empty<Type>();

        public virtual bool IsCompatibleWith(IInterestPoint other)
        {
            return !IncompatibleTypes.Contains(other.GetType());
        }
    }
}
