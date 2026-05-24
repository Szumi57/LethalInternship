using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class GatheringPointRenderer : IInterestPointRenderer<GatheringInterestPoint>
    {
        public EnumIconImagesTypes GetIconImagesTypes(GatheringInterestPoint interestPoint)
        {
            return EnumIconImagesTypes.GatheringPoint;
        }

        public Vector3 GetUIPos(GatheringInterestPoint interestPoint)
        {
            return interestPoint.Point;
        }
    }
}
