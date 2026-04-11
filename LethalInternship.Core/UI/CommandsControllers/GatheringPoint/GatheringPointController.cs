using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class GatheringPointController : MonoBehaviour, IVisibilityUI
    {
        public System.Action<EnumInputAction> OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IVisibilityUI.GroupUI => this.GroupUI;

        public Image BgImage = null!;
        public Image BgRemoveButtonImage = null!;

        public Image SetImage = null!;
        public Image GoToImage = null!;

        public TextMeshProUGUI TMPDescription = null!;

        public RemoveGatheringPointController removeGatheringPointController = null!;

        float transparency = 1f;

        // ?
        bool gatheringPointSet = false;

        // Typing animation
        private string fullText = string.Empty;
        private string cursorChar = "$";
        private float cursorBlink = 0.2f;
        private Coroutine typingCoroutine = null!;
        private Coroutine cursorCoroutine = null!;
        private bool showCursor = true;
        private string currentText = string.Empty;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : "GatheringPointController";

        void Awake()
        {
            SetAlpha(SetImage, 1f);
            SetAlpha(GoToImage, 0f);
            Go = this.gameObject;
        }

        void OnEnable()
        {
            SetButtonNotHovered();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);

            UpdateStateRemoveButton();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
                SetAlpha(GetCurrentImage(), 0.2f);
            else
                SetAlpha(GetCurrentImage(), transparency);
        }

        private Image GetCurrentImage()
        {
            return gatheringPointSet ? GoToImage : SetImage;
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

        private EnumInputAction GetCurrentInputAction()
        {
            return gatheringPointSet ? EnumInputAction.GoToGatheringPoint : EnumInputAction.SetGatheringPoint;
        }

        private void SetTMPDescriptionFont(TMP_FontAsset font)
        {
            if (TMPDescription != null)
            {
                TMPDescription.font = font;
            }
        }

        private void SetButtonHovered()
        {
            SetAlpha(BgImage, 1f);

            if (gatheringPointSet)
            {
                SetAlpha(BgRemoveButtonImage, 1f);
            }

            fullText = UIConst.COMMANDS_BUTTON_STRING[(int)GetCurrentInputAction()];
            if (TMPDescription != null)
            {
                TMPDescription.text = "";
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        private void SetButtonNotHovered()
        {
            SetAlpha(BgImage, 0f);

            // Typing animation
            StopAllCoroutines();
            if (TMPDescription != null)
            {
                TMPDescription.text = string.Empty;
                currentText = string.Empty;
            }
        }

        private void UpdateIconAndDesc()
        {
            if (gatheringPointSet)
            {
                SetAlpha(GoToImage, 1f);
                SetAlpha(SetImage, 0f);
            }
            else
            {
                SetAlpha(GoToImage, 0f);
                SetAlpha(SetImage, 1f);
            }
        }

        private void UpdateStateRemoveButton()
        {
            if (removeGatheringPointController != null)
            {
                removeGatheringPointController.gameObject.SetActive(gatheringPointSet);
            }
        }

        IEnumerator TypeText()
        {
            foreach (char c in fullText)
            {
                currentText += c;
                UpdateText();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
            }
        }

        IEnumerator CursorBlink()
        {
            while (currentText != fullText)
            {
                showCursor = !showCursor;
                UpdateText();
                yield return new WaitForSeconds(cursorBlink);
            }
            // No cursor after the end
            showCursor = false;
            UpdateText();
        }

        void UpdateText()
        {
            TMPDescription.text = currentText + (showCursor ? cursorChar : " ");
        }

        #region Events

        public void Selected()
        {
            if (isNotInteractable) return;

            gatheringPointSet = !gatheringPointSet;

            OnSelected?.Invoke(GetCurrentInputAction());

            UpdateIconAndDesc();
            UpdateStateRemoveButton();
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

        #endregion
    }
}
