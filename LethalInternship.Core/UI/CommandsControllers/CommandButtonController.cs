using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandButtonController : MonoBehaviour
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

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

        public bool IsNotAvailable;

        void Awake()
        {
            if ((int)TypeInputAction < UIConst.COMMANDS_BUTTON_STRING.Length)
            {
                fullText = UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction];
            }
        }

        void OnEnable()
        {
            SetButtonNotHovered();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);
        }

        void Start()
        {
            SetAlpha(IconImage, 1f);
            SetButtonNotHovered();
        }

        // Update is called once per frame
        void Update()
        {
            // Transparency
            if (IsNotAvailable)
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

        public void Selected()
        {
            OnSelected?.Invoke(TypeInputAction);
        }

        public void MouseOver()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonNotHovered();
        }
    }
}
