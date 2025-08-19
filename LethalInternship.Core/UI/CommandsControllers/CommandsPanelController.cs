using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandsPanelController : MonoBehaviour
    {
        public CommandButtonController[] CommandButtons;
        public TextMeshProUGUI CommandDescription;

        // Start is called before the first frame update
        void Start()
        {
            CommandButtons = GetComponentsInChildren<CommandButtonController>();
            if (CommandButtons == null
                || CommandButtons.Length == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke("No CommandButtons found !");
            }

            if (CommandDescription == null)
            {
                CommandDescription = GetComponent<TextMeshProUGUI>();
            }
            if (CommandDescription == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("No CommandDescription found !");
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (CommandButtons == null
                || CommandButtons.Length == 0)
            {
                return;
            }

            // Change description
            bool oneIsHovered = false;
            foreach (CommandButtonController button in CommandButtons)
            {
                if (button.IsHovered)
                {
                    oneIsHovered = true;

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
            }

            if (!oneIsHovered)
            {
                CommandDescription.text = string.Empty;
            }
        }

        public CommandButtonController? GetGoToVehicleButton()
        {
            if (CommandButtons != null)
            {
                return CommandButtons.FirstOrDefault(x => x.ID == (int)EnumInputAction.GoToVehicle);
            }
            return null;
        }

        public void SetFont(TMP_FontAsset font)
        {
            CommandDescription.font = font;
        }
    }
}
