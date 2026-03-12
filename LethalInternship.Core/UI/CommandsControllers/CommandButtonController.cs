using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandButtonController : MonoBehaviour
    {
        public System.Action<EnumInputAction> OnSelected = null!;

        public EnumInputAction TypeInputAction;
        public Image BgImage = null!;
        public Image IconImage = null!;
        public TextMeshProUGUI TMPDescription = null!;

        private float transparency = 1f;

        public bool IsNotAvailable;

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
                SetAlpha(IconImage, 0.2f);
            }
            else
            {
                SetAlpha(IconImage, transparency);
                SetAlpha(IconImage, transparency);
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

        private void SetTMPDescriptionText(string text)
        {
            if (TMPDescription != null)
            {
                TMPDescription.text = text;
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
            if ((int)TypeInputAction < UIConst.COMMANDS_BUTTON_STRING.Length)
            {
                SetTMPDescriptionText(UIConst.COMMANDS_BUTTON_STRING[(int)TypeInputAction]);
            }
        }

        private void SetButtonNotHovered()
        {
            SetAlpha(BgImage, 0f);
            SetTMPDescriptionText(string.Empty);
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
