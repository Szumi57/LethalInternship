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

        public Vector3 GetUIPos(ShipInterestPoint interestPoint)
        {
            return interestPoint.HangarShipTransform.transform.position + interestPoint.HangarShipTransform.rotation * new Vector3(0f, 6f, -7f);
        }
    }
}
