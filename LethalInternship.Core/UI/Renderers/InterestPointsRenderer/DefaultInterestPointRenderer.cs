using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class DefaultInterestPointRenderer : IInterestPointRenderer<DefaultInterestPoint>
    {
        public GameObject GetImagePrefab(DefaultInterestPoint defaultInterestPoint)
        {
            return PluginRuntimeProvider.Context.PedestrianIconImagePrefab;
        }

        public Vector3 GetUIPos(DefaultInterestPoint interestPoint)
        {
            return interestPoint.Point;
        }
    }
}
