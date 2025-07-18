using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers
{
    public class InterestPointRendererWrapper<T> : IInterestPointRendererWrapper where T : IInterestPoint
    {
        private readonly IInterestPointRenderer<T> renderer;

        public InterestPointRendererWrapper(IInterestPointRenderer<T> renderer)
        {
            this.renderer = renderer;
        }

        public GameObject GetImagePrefab(IInterestPoint interestPoint)
        {
            return renderer.GetImagePrefab((T)interestPoint);
        }
    }
}
