using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class RemoveGatheringPointController : MonoBehaviour, IVisibilityUI
    {
        public static System.Action OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IVisibilityUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction { get; } = EnumInputAction.RemoveGatheringPoint;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        private float transparency = 1f;
        private float transparencyNotInteractable = 0.2f;
        private float holdTime = 0.5f;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

        void Awake()
        {
            Go = this.gameObject;
        }

        void OnEnable()
        {
            SetButtonNotHovered();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
            {
                SetAlpha(IconImage, transparencyNotInteractable);
                SetAlpha(FrameImage, transparencyNotInteractable);
            }
            else
            {
                SetAlpha(IconImage, transparency);
                SetAlpha(FrameImage, transparency);
            }
        }

        private string SetTooltipMessage()
        {
            return UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction];
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
            UIManager.Instance.ToolTipBarUI.Hide();

            if (isNotInteractable) return;

            OnSelected?.Invoke();
        }

        public void PointerDown()
        {
            if (isNotInteractable) return;
            UIManager.Instance.ToolTipBarUI.StartHold(holdTime, ActionValidated);
        }

        public void PointerUp()
        {
            if (isNotInteractable) return;
            UIManager.Instance.ToolTipBarUI.StopHold();
            SetButtonNotHovered();
        }

        public void MouseOver()
        {
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);
            if (isNotInteractable) return;
            SetButtonHovered();
        }

        public void MouseLeave()
        {
            UIManager.Instance.ToolTipBarUI.Hide();
            SetButtonNotHovered();
        }
    }
}
