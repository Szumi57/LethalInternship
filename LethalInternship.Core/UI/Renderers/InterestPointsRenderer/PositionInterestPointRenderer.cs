using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers.InterestPointsRenderer
{
    public class PositionInterestPointRenderer : IInterestPointRenderer<PositionInterestPoint>
    {
        public EnumIconImagesTypes GetIconImagesTypes(PositionInterestPoint interestPoint)
        {
            return EnumIconImagesTypes.Position;
        }

        public Vector3 GetUIPos(PositionInterestPoint interestPoint)
        {
            return interestPoint.Point;
        }
    }
}
