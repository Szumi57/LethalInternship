using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class PositionInterestPointRenderer : IInterestPointRenderer<PositionInterestPoint>
    {
        public EnumIconImagesTypes GetIconImagesTypes(PositionInterestPoint interestPoint)
        {
            return EnumIconImagesTypes.Position;
        }

        public GameObject GetImagePrefab(PositionInterestPoint defaultInterestPoint)
        {
            return PluginRuntimeProvider.Context.PositionIconImagePrefab;
        }

        public Vector3 GetUIPos(PositionInterestPoint interestPoint)
        {
            return interestPoint.Point;
        }
    }
}
