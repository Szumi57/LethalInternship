using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class RemoveGatheringPointController : MonoBehaviour
    {
        public System.Action<EnumInputAction> OnSelected = null!;

        public EnumInputAction TypeInputAction;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        public bool IsNotAvailable;

        float transparency = 1f;

        void OnEnable()
        {
            SetButtonNotHovered();
        }

        void Update()
        {
            // Transparency
            if (IsNotAvailable)
            {
                SetAlpha(IconImage, 0.2f);
                SetAlpha(FrameImage, 0.2f);
            }
            else
            {
                SetAlpha(IconImage, transparency);
                SetAlpha(FrameImage, transparency);
            }
        }

        private void SetAlpha(Image image, float transparency)
        {
            if (image != null
                && image.color.a != transparency)
            {
                Color alpha = image.color;
                alpha.a = transparency;
                image.color = alpha;
            }
        }

        private void SetButtonHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 10f;
        }

        private void SetButtonNotHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 25f;
        }

        public void Selected()
        {
            OnSelected?.Invoke(TypeInputAction);
        }

        public void MouseOver()
        {
            SetButtonHovered();
        }

        public void MouseLeave()
        {
            SetButtonNotHovered();
        }
    }
}
