using LethalInternship.Core.UI.TooltipBar;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
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

        private float transparency = 1f;
        private float holdTime = 1.2f;

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

        private void ActionValidated()
        {
            TooltipBarUI.Instance.Hide();
            OnSelected?.Invoke(TypeInputAction);

            PluginLoggerHook.LogDebug?.Invoke($"RemoveButtonSelected {TypeInputAction} click !");
        }

        public void PointerDown()
        {
            TooltipBarUI.Instance.StartHold(holdTime, ActionValidated);
        }

        public void MouseOver()
        {
            TooltipBarUI.Instance.RequestShow("Hold !!");

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            TooltipBarUI.Instance.Hide();
            SetButtonNotHovered();
        }

        public void PointerUp()
        {
            TooltipBarUI.Instance.StopHold();
            SetButtonNotHovered();
        }
    }
}
