using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.DualSwitch
{
    public class ButtonDualSwitchChildController : MonoBehaviour
    {
        public System.Action<EnumClickSide> OnSideSelected = null!;

        public EnumClickSide side;

        public Image FrameImage = null!;
        public Image IconImage = null!;

        float transparency = 1f;

        public bool IsNotAvailable;

        void Start()
        {
            SetAlpha(IconImage, 1f);
            SetButtonNotHovered();
        }

        void OnEnable()
        {
            SetButtonNotHovered();
        }

        // Update is called once per frame
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

        public void Selected()
        {
            OnSideSelected?.Invoke(side);
        }

        public void MouseOver()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonNotHovered();
        }

        private void SetButtonHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 7f;
        }

        private void SetButtonNotHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 15f;
        }
    }
}
