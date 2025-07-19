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
    }
}
