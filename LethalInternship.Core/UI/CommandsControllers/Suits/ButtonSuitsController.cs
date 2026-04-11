using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSuitsController : MonoBehaviour, IVisibilityUI
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IVisibilityUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction;
        public Image FrameImage = null!;
        public Image IconImage = null!;

        private float transparencyFull = 1f;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : "ButtonSuitsController";

        void Awake()
        {
            Go = this.gameObject;
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
                SetAlpha(IconImage, 0.2f);
            else
                SetAlpha(IconImage, transparencyFull);
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
