using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints
{
    public class VehicleInterestPoint : InterestPointBase
    {
        public VehicleController VehicleController => vehicleController;
        private VehicleController vehicleController;

        public override Vector3 Point => GetVehiclePoint(vehicleController);
        protected override IEnumerable<Type> IncompatibleTypes => new[] { typeof(DefaultInterestPoint), typeof(ShipInterestPoint) };
        public override EnumCommandTypes? CommandType => EnumCommandTypes.GoToVehicle;
        public override bool IsInvalid => vehicleController == null || vehicleController.carDestroyed;

        public VehicleInterestPoint(VehicleController vehicle)
        {
            this.vehicleController = vehicle;
        }

        public static Vector3 GetVehiclePoint(VehicleController vehicleController)
        {
            if (vehicleController == null)
            {
                return default(Vector3);
            }

            return vehicleController.transform.position;
        }
    }
}
