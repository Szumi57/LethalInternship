using LethalInternship.Core.Interns.AI.PointsOfInterest;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public IPointOfInterest? GatheringPoint { get; private set; } = null;

        private List<IPointOfInterest> listPointOfInterest = new List<IPointOfInterest>();

        public void SetGatheringPoint(IPointOfInterest? point)
        {
            if (point == null
                && GatheringPoint != null)
            {
                GatheringPoint.TryRemoveInterestPointType(typeof(GatheringInterestPoint));
            }

            GatheringPoint = point;
        }

        #region Points of interest

        public bool CheckAndClearInvalidPointOfInterest(IPointOfInterest? pointOfInterest)
        {
            if (pointOfInterest != null && pointOfInterest.IsInvalid)
            {
                listPointOfInterest.Remove(pointOfInterest);
                return true;
            }

            return false;
        }

        public IPointOfInterest GetPointOfInterestOrNewPositionPoint(Vector3 pos)
        {
            return GetOrCreatePointOfInterest(pos, () => new PositionInterestPoint(pos));
        }

        public IPointOfInterest GetPointOfInterestOrNewVehiclePoint(VehicleController vehicleController)
        {
            Vector3 vehiclePoint = VehicleInterestPoint.GetVehiclePoint(vehicleController);
            return GetOrCreatePointOfInterest(vehiclePoint, () => new VehicleInterestPoint(vehicleController));
        }

        public IPointOfInterest GetPointOfInterestOrNewShipPoint(Transform shipTransform)
        {
            Vector3 shipPoint = ShipInterestPoint.GetShipPoint(shipTransform);
            return GetOrCreatePointOfInterest(shipPoint, () => new ShipInterestPoint(shipTransform));
        }

        public IPointOfInterest GetPointOfInterestOrNewGatheringPoint(Vector3 pos)
        {
            return GetOrCreatePointOfInterest(pos, () => new GatheringInterestPoint(pos));
        }

        private IPointOfInterest GetOrCreatePointOfInterest(Vector3 keyPoint, Func<IInterestPoint> interestPointFactory)
        {
            var pointOfInterest = listPointOfInterest.FirstOrDefault(x => !x.IsInvalid && x.GetPoint() == keyPoint);
            if (pointOfInterest == null)
            {
                pointOfInterest = new PointOfInterest();
                listPointOfInterest.Add(pointOfInterest);
            }

            pointOfInterest.TryAddInterestPoint(interestPointFactory());

            PluginLoggerHook.LogDebug?.Invoke($"listPointOfInterest {listPointOfInterest.Count}");
            foreach (var point in listPointOfInterest)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- POI :");
                foreach (var ip in point.GetListInterestPoints())
                    PluginLoggerHook.LogDebug?.Invoke($"       ip {ip.GetType()}");
            }
            return pointOfInterest;
        }

        #endregion
    }
}
