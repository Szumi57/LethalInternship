using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class RemoveGatheringPointController : MonoBehaviour,
        IVisibilityUI,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler,
        IHoldHandler
    {
        public static System.Action OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.GatheringPointGroupButtons;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction { get; } = EnumInputAction.RemoveGatheringPoint;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        private float transparency = 1f;
        private float transparencyNotInteractable = 0.2f;
        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;
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
            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();
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
            UpdateHighlight(forceUpdate: true);
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

        private void StartHover()
        {
            FrameImage.pixelsPerUnitMultiplier = 10f;
        }

        private void StopHover()
        {
            FrameImage.pixelsPerUnitMultiplier = 25f;
        }

        private void UpdateHighlight(bool forceUpdate = false)
        {
            bool highlighted = isPointerOver || isSelected;

            if (highlighted == isHovered
                && !forceUpdate)
                return;

            isHovered = highlighted;

            if (isHovered)
                StartHover();
            else
                StopHover();
        }

        public void OnHoldStart()
        {
            UIManager.Instance.ToolTipBarUI.StartHold(holdTime, ActionValidated);
        }

        public void OnHoldEnd()
        {
            UIManager.Instance.ToolTipBarUI.StopHold();
        }

        private void ActionValidated()
        {
            UIManager.Instance.ToolTipBarUI.Hide();

            if (isNotInteractable) return;

            OnSelected?.Invoke();
        }

        #region Mouse events

        public void OnPointerEnter(PointerEventData eventData)
        {
            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = true;
            isSelected = false;

            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);
            UpdateHighlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            isSelected = false;

            UIManager.Instance.ToolTipBarUI.Hide();
            UpdateHighlight();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isNotInteractable)
                return;

            OnHoldStart();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isNotInteractable)
                return;

            OnHoldEnd();
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
            if (!InputManager.Instance.IsUsingController)
                return;

            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = false;
            isSelected = true;
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);
            UpdateHighlight();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isPointerOver = false;
            isSelected = false;

            UIManager.Instance.ToolTipBarUI.Hide();
            UpdateHighlight();
        }

        #endregion
    }
}
