using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public class VehicleInterestPoint : InterestPointBase
    {
        public override Vector3 Point => GetVehiclePoint(vehicleController);

        private VehicleController vehicleController;
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(DefaultInterestPoint), typeof(ShipInterestPoint) };
        public override EnumCommandTypes? CommandType => EnumCommandTypes.GoToVehicle;

        public VehicleInterestPoint(VehicleController vehicle)
        {
            this.vehicleController = vehicle;
        }

        public static Vector3 GetVehiclePoint(VehicleController vehicleController)
        {
            return vehicleController.transform.position + new Vector3(0f, 2f, 0f);
        }
    }
}
