using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public class DefaultInterestPoint : InterestPointBase
    {
        public override Vector3 Point => point;

        private Vector3 point;
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(VehicleInterestPoint) };

        public DefaultInterestPoint(Vector3 position)
        {
            point = position;
        }
    }
}
