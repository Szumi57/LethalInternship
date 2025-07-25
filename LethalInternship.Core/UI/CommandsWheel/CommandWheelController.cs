using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.CommandButton;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalInternship.Core.UI.CommandsWheel
{
    public class CommandWheelController : MonoBehaviour
    {
        public CommandButtonController[] CommandButtons;
        public Sprite[] UsedSpritesInAnimation;
        public TextMeshProUGUI CommandDescription;
        public RectTransform CommandWheelRectTransform;

        private CommandButtonController closestButton;
        private RectTransform CanvasOverlayRectTransform;
        private TMP_FontAsset font;

        // Start is called before the first frame update
        void Start()
        {
            CommandButtons = GetComponentsInChildren<CommandButtonController>();
            if (CommandButtons == null
                || CommandButtons.Length == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke("No CommandButtons found !");
            }

            if (UsedSpritesInAnimation == null
                || UsedSpritesInAnimation.Length == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke("No UsedSpritesInAnimation found !");
            }

            if (CommandDescription == null)
            {
                CommandDescription = GetComponent<TextMeshProUGUI>();
            }
            if (CommandDescription == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("No CommandDescription found !");
            }

            CanvasOverlayRectTransform = UIManager.Instance.CanvasOverlay.GetComponentInChildren<RectTransform>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (CommandButtons == null
                || CommandButtons.Length == 0)
            {
                return;
            }

            if (UsedSpritesInAnimation == null
                || UsedSpritesInAnimation.Length == 0)
            {
                return;
            }

            // Get closest button
            closestButton = GetClosestButton();

            // Change description
            foreach (CommandButtonController button in CommandButtons)
            {
                if (button == closestButton)
                {
                    button.CommandFrameImage.sprite = UsedSpritesInAnimation[(int)SpriteForAnimation.WheelButtonFrameSelected];
                    button.CommandIcon.color = new Color(0f, 0f, 0f);

                    // Description
                    if (CommandDescription == null)
                    {
                        continue;
                    }

                    if (UIConst.COMMANDS_BUTTON_STRING.Length <= button.ID)
                    {
                        CommandDescription.text = string.Empty;
                    }
                    else
                    {
                        CommandDescription.text = UIConst.COMMANDS_BUTTON_STRING[button.ID].ToString();
                    }
                }
                else
                {
                    button.CommandFrameImage.sprite = UsedSpritesInAnimation[(int)SpriteForAnimation.WheelButtonFrameUnselected];
                    button.CommandIcon.color = new Color(255 / 255f, 255 / 255f, 255 / 255f);
                }
            }
        }

        private CommandButtonController GetClosestButton()
        {
            float minDist = float.MaxValue;
            CommandButtonController minDistButton = CommandButtons[0];
            foreach (var button in CommandButtons)
            {
                if (button.IsNotAvailable)
                {
                    continue;
                }

                Vector2 offset = new Vector2(CommandWheelRectTransform.transform.localPosition.x, CommandWheelRectTransform.transform.localPosition.y);
                Vector2 buttonPos = new Vector2(button.transform.localPosition.x, button.transform.localPosition.y) + offset;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasOverlayRectTransform, Mouse.current.position.ReadValue(), null, out Vector2 realMousePos);
                float dist = (realMousePos - buttonPos).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    minDistButton = button;
                }

                //RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasOverlayRectTransform, new Vector2(button.transform.localPosition.x, button.transform.localPosition.y), null, out Vector2 realButtonPos);
                //RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasOverlayRectTransform, Mouse.current.position.ReadValue(), null, out Vector2 realMousePos);
                //float dist = (realMousePos - realButtonPos).sqrMagnitude;
                //if (dist < minDist)
                //{
                //    minDist = dist;
                //    minDistButton = button;
                //}
            }

            return minDistButton;
        }

        public CommandButtonController? GetGoToVehicleButton()
        {
            if (CommandButtons != null)
            {
                return CommandButtons.FirstOrDefault(x => x.ID == (int)EnumInputAction.GoToVehicle);
            }
            return null;
        }

        public void CommandWheelMouseDown()
        {
            closestButton.Selected();
        }

        public void SetFont(TMP_FontAsset font)
        {
            this.font = font;
            CommandDescription.font = font;
        }
    }

    public enum SpriteForAnimation
    {
        WheelButtonFrameUnselected,
        WheelButtonFrameSelected
    }
}
