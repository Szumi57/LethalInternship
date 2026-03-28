using LethalInternship.Core.UI.TooltipBar;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSuitsController : MonoBehaviour
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

        public EnumInputAction TypeInputAction;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        public bool IsNotAvailable;

        private float transparencyFull = 1f;

        void Update()
        {
            // Transparency
            if (IsNotAvailable)
            {
                SetAlpha(IconImage, 0.2f);
            }
            else
            {
                SetAlpha(IconImage, transparencyFull);
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
            FrameImage.pixelsPerUnitMultiplier = 20f;
        }

        public void Selected()
        {
            OnSelected?.Invoke(TypeInputAction);
        }

        public void MouseOver()
        {
            TooltipBarUI.Instance.RequestShow("Suit test");

            if (IsNotAvailable) return;

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            TooltipBarUI.Instance.Hide();

            if (IsNotAvailable) return;

            SetButtonNotHovered();
        }
    }
}
