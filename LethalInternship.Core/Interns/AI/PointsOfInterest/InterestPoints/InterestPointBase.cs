using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public abstract class InterestPointBase : IInterestPoint
    {
        public abstract Vector3 Point { get; }
        public abstract EnumCommandTypes? CommandType { get; }

        protected virtual IEnumerable<Type> IncompatibleTypes => Enumerable.Empty<Type>();

        public virtual bool IsCompatibleWith(IInterestPoint other)
        {
            return !IncompatibleTypes.Contains(other.GetType());
        }
    }
}
