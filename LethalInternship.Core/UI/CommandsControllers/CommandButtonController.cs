using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandButtonController : MonoBehaviour, IVisibilityUI
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IVisibilityUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction;
        public Image BgImage = null!;
        public Image IconImage = null!;
        public TextMeshProUGUI TMPDescription = null!;

        private float transparencyFull = 1f;

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
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : "CommandButtonController";

        void Awake()
        {
            if ((int)TypeInputAction < UIConst.COMMANDS_BUTTON_STRING.Length)
            {
                fullText = UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction];
            }
            Go = this.gameObject;
        }

        void OnEnable()
        {
            SetButtonNotHovered();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
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
            StopAllCoroutines();
            if (TMPDescription != null)
            {
                TMPDescription.text = string.Empty;
                currentText = string.Empty;
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

        #endregion
    }
}
