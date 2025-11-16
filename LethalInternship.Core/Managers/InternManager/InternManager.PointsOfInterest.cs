using LethalInternship.Core.Interns.AI.PointsOfInterest;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        private List<IPointOfInterest> listPointOfInterest = new List<IPointOfInterest>();

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

        public IPointOfInterest GetPointOfInterestOrDefaultInterestPoint(Vector3 pos)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == pos);
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new DefaultInterestPoint(pos));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        public IPointOfInterest GetPointOfInterestOrVehicleInterestPoint(VehicleController vehicleController)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == VehicleInterestPoint.GetVehiclePoint(vehicleController));
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new VehicleInterestPoint(vehicleController));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        public IPointOfInterest GetPointOfInterestOrShipInterestPoint(Transform shipTransform)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == ShipInterestPoint.GetShipPoint(shipTransform));
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new ShipInterestPoint(shipTransform));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        #endregion
    }
}
