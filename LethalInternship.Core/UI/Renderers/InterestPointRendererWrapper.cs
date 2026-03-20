using LethalInternship.SharedAbstractions.Enums;
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

        public EnumIconImagesTypes GetIconImagesTypes(IInterestPoint interestPoint)
        {
            return renderer.GetIconImagesTypes((T)interestPoint);
        }

        public GameObject GetImagePrefab(IInterestPoint interestPoint)
        {
            return renderer.GetImagePrefab((T)interestPoint);
        }

        public Vector3 GetUIPos(IInterestPoint interestPoint)
        {
            return renderer.GetUIPos((T)interestPoint);
        }
    }
}
