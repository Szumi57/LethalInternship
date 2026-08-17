using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.ItemBlocks
{
    public class ItemButtonController : MonoBehaviour
    {
        public static System.Action<GrabbableObject, EnumInputAction> OnSelected = null!;

        public Image BGImage = null!;
        public Image ButtonImage = null!;
        public EnumInputAction TypeInputAction;

        private GrabbableObject itemGrabbableObject = null!;
        private float holdTime = 0.3f;

        void OnEnable()
        {
            SetButtonNotHovered();
        }

        public void Init(GrabbableObject itemGrabbableObject)
        {
            this.itemGrabbableObject = itemGrabbableObject;
        }

        private void SetButtonHovered()
        {
            SetAlpha(BGImage, 1f);
            SetAlpha(ButtonImage, 1f);
        }

        private void SetButtonNotHovered()
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

        #region Events

        private void ActionValidated()
        {
            UIManager.Instance.ToolTipBarUI.Hide();
            OnSelected?.Invoke(itemGrabbableObject, TypeInputAction);
        }

        public void PointerDown()
        {
            UIManager.Instance.ToolTipBarUI.StartHold(holdTime, ActionValidated);
        }

        public void PointerUp()
        {
            UIManager.Instance.ToolTipBarUI.StopHold();
        }

        public void MouseOver()
        {
            UIManager.Instance.ToolTipBarUI.ShowImmediate($"{UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction]}");
            SetButtonHovered();
        }

        public void MouseLeave()
        {
            UIManager.Instance.ToolTipBarUI.Hide();
            SetButtonNotHovered();
        }

        #endregion
    }
}
