using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.ItemBlocks
{
    public class ItemButtonController : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler,
        IHoldHandler
    {
        public static System.Action<GrabbableObject, EnumInputAction> OnSelected = null!;

        public Image BGImage = null!;
        public Image ButtonImage = null!;
        public EnumInputAction TypeInputAction;

        private GrabbableObject itemGrabbableObject = null!;
        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;
        private float holdTime = 0.3f;

        void OnEnable()
        {
            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();
        }

        public void Init(GrabbableObject itemGrabbableObject)
        {
            this.itemGrabbableObject = itemGrabbableObject;
        }

        private void StartHover()
        {
            SetAlpha(BGImage, 1f);
            SetAlpha(ButtonImage, 1f);
        }

        private void StopHover()
        {
            SetAlpha(BGImage, 0.39f);
            SetAlpha(ButtonImage, 0.39f);
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

        public bool StayActive(bool isCurrentWeapon)
        {
            switch (TypeInputAction)
            {
                case EnumInputAction.SwapWeapon:
                    return !isCurrentWeapon && InternManager.Instance.IsItemUsableWeapon(itemGrabbableObject);

                case EnumInputAction.ActivateItem:
                    return InternManager.Instance.IsItemUsableItem(itemGrabbableObject);

                case EnumInputAction.DropItem:
                    return true;
            }

            return true;
        }

        private void UpdateHighlight()
        {
            bool highlighted = isPointerOver || isSelected;

            if (highlighted == isHovered)
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
            OnSelected?.Invoke(itemGrabbableObject, TypeInputAction);
        }

        #region Mouse events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (InputManager.Instance.IsUsingController)
                return;

            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = true;
            isSelected = false;
            UIManager.Instance.ToolTipBarUI.ShowImmediate($"{UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction]}");
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
            OnHoldStart();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnHoldEnd();
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = false;
            isSelected = true;
            UIManager.Instance.ToolTipBarUI.ShowImmediate($"{UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction]}");
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
