using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSuitsController : MonoBehaviour,
        IVisibilityUI,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.SuitMenu;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        private float transparencyFull = 1f;

        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;

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
                SetAlpha(IconImage, 0.2f);
            else
                SetAlpha(IconImage, transparencyFull);

            UpdateHighlight(forceUpdate: true);
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
            FrameImage.pixelsPerUnitMultiplier = 20f;
        }

        private string SetTooltipMessage()
        {
            return UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction];
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isNotInteractable) return;
            OnSelected?.Invoke(TypeInputAction);
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
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

        public void OnSubmit(BaseEventData eventData)
        {
            if (isNotInteractable) return;
            OnSelected?.Invoke(TypeInputAction);
        }

        #endregion
    }
}
