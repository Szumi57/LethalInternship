using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Interns.AI.PointsOfInterest.InterestPoints
{
    public class VehicleInterestPoint : InterestPointBase
    {
        public override Vector3 Point => point;

        private Vector3 point;
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(DefaultInterestPoint) };

        public VehicleInterestPoint(VehicleController vehicle)
        {
            point = vehicle.transform.position;
        }
    }
}
