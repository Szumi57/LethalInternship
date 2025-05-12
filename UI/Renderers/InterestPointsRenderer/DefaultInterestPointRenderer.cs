using LethalInternship.Interfaces;
using LethalInternship.Interns.AI.PointsOfInterest.InterestPoints;
using UnityEngine;

namespace LethalInternship.UI.Renderers.InterestPointsRenderer
{
    public class DefaultInterestPointRenderer : IInterestPointRenderer<DefaultInterestPoint>
    {
        public GameObject GetImagePrefab(DefaultInterestPoint defaultInterestPoint)
        {
            return Plugin.DefaultIconImagePrefab;
        }
    }
}
