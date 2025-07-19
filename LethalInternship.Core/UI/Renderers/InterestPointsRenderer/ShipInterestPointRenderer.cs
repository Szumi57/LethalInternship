using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class ShipInterestPointRenderer : IInterestPointRenderer<ShipInterestPoint>
    {
        public GameObject GetImagePrefab(ShipInterestPoint interestPoint)
        {
            return PluginRuntimeProvider.Context.ShipIconImagePrefab;
        }
    }
}
