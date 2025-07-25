using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class VehicleInterestPointRenderer : IInterestPointRenderer<VehicleInterestPoint>
    {
        public GameObject GetImagePrefab(VehicleInterestPoint interestPoint)
        {
            return PluginRuntimeProvider.Context.VehicleIconImagePrefab;
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
