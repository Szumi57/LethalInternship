using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public class GatheringInterestPoint : InterestPointBase
    {
        public override Vector3 Point => position;

        private Vector3 position;
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(VehicleInterestPoint), typeof(ShipInterestPoint) };
        public override EnumCommandTypes? CommandType => EnumCommandTypes.GoToPosition;

        public override bool IsInvalid => false;

        public GatheringInterestPoint(Vector3 position)
        {
            this.position = position;
        }
    }
}
