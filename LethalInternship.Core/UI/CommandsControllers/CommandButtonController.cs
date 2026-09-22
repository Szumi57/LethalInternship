using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandButtonController : MonoBehaviour,
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
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public EnumInputAction TypeInputAction;
        public Image BgImage = null!;
        public Image IconImage = null!;
        public TextMeshProUGUI TMPDescription = null!;

        private float transparencyFull = 1f;
        private float transparencyNotInteractable = 0.2f;

        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;

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
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

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
            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
                SetAlpha(IconImage, transparencyNotInteractable);
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

        private void SetTMPDescriptionFont(TMP_FontAsset font)
        {
            if (TMPDescription != null)
            {
                TMPDescription.font = font;
            }
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

        private void StartHover()
        {
            SetAlpha(BgImage, isNotInteractable ? transparencyNotInteractable : transparencyFull);
            StopTypeTextCoroutine();
            if (TMPDescription != null)
            {
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        private void StopHover()
        {
            SetAlpha(BgImage, 0f);
            StopTypeTextCoroutine();
        }

        private void StopTypeTextCoroutine()
        {
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

        private string SetTooltipMessage()
        {
            return fullText;
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
