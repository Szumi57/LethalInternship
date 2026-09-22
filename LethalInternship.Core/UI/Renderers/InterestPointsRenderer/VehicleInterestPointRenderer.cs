using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class VehicleInterestPointRenderer : IInterestPointRenderer<VehicleInterestPoint>
    {
        public EnumIconImagesTypes GetIconImagesTypes(VehicleInterestPoint interestPoint)
        {
            return EnumIconImagesTypes.Vehicle;
        }

        public Vector3 GetUIPos(VehicleInterestPoint interestPoint)
        {
            if (interestPoint.IsInvalid)
            {
                return default(Vector3);
            }

            return interestPoint.VehicleController.transform.position + new Vector3(0f, 3f, 0f); // no rotation need with just y
        }
    }
}
