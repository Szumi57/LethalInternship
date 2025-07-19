using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public class ShipInterestPoint : InterestPointBase
    {
        public override Vector3 Point => GetShipPoint(hangarShipTransform);

        private Transform hangarShipTransform;
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(DefaultInterestPoint), typeof(VehicleInterestPoint) };
        public override EnumCommandTypes? CommandType => EnumCommandTypes.GoToPosition;

        public ShipInterestPoint(Transform hangarShipTransform)
        {
            this.hangarShipTransform = hangarShipTransform;
        }

        public static Vector3 GetShipPoint(Transform hangarShipTransform)
        {
            return hangarShipTransform.position;
        }
    }
}
