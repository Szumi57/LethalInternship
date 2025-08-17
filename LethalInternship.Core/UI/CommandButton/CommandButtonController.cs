using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandButton
{
    public class CommandButtonController : MonoBehaviour
    {
        public event EventHandler OnSelected = null!;

        public int ID;
        public Image CommandFrameImage;
        public Image CommandIcon;
        public Sprite[] UsedSpritesInAnimation;

        public bool IsNotAvailable;
        public bool IsHovered;

        // Start is called before the first frame update
        void Start()
        {
            if (CommandFrameImage == null)
            {
                CommandFrameImage = GetComponent<Image>();
            }
            CommandFrameImage.sprite = UsedSpritesInAnimation[(int)SpriteForAnimation.WheelButtonFrameSelected];
            SetTransparency(CommandFrameImage, 0f);

            if (CommandIcon == null)
            {
                CommandIcon = GetComponentInChildren<Image>();
            }

            if (UsedSpritesInAnimation == null
                || UsedSpritesInAnimation.Length == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke("No UsedSpritesInAnimation found !");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (UsedSpritesInAnimation == null
                || UsedSpritesInAnimation.Length == 0)
            {
                return;
            }

            // Transparency
            float transparency = 1f;
            if (IsNotAvailable)
            {
                transparency = 0.2f;
            }

            if (CommandIcon != null
                && CommandIcon.color.a != transparency)
            {
                SetTransparency(CommandIcon, transparency);
            }
        }

        private void SetTransparency(Image image, float transparency)
        {
            Color alpha = image.color;
            alpha.a = transparency;
            image.color = alpha;
        }

        public void Selected()
        {
            OnSelected?.Invoke(this, null);
        }

        public void MouseOver()
        {
            if (IsNotAvailable)
            {
                return;
            }

            IsHovered = true;

            SetTransparency(CommandFrameImage, 1f);
            CommandIcon.color = new Color(0f, 0f, 0f);
        }

        public void MouseLeave()
        {
            if (IsNotAvailable)
            {
                return;
            }

            IsHovered = false;

            SetTransparency(CommandFrameImage, 0f);
            CommandIcon.color = new Color(255 / 255f, 255 / 255f, 255 / 255f);
        }
    }

    public enum SpriteForAnimation
    {
        WheelButtonFrameUnselected,
        WheelButtonFrameSelected
    }
}
