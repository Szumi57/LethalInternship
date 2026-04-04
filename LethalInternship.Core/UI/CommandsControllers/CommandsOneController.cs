using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandsOneController : MonoBehaviour
    {
        public CommandButtonController[] CommandButtons = null!;

        public TextMeshProUGUI TitleUI = null!;
        public TextMeshProUGUI ModNamePanelDescription = null!;

        private Coroutine CoroutineUpdateCommandsUI = null!;

        IInternIdentity currentCommandIdentity = null!;

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;
            SetTitleUIFont(fontToUse);
            SetModNamePanelDescriptionFont(fontToUse);

            // Update commands UI while displaying
            if (CoroutineUpdateCommandsUI != null)
            {
                StopCoroutine(CoroutineUpdateCommandsUI);
            }
            CoroutineUpdateCommandsUI = StartCoroutine(UpdateCommandsUI());

            IInternIdentity? identity = IdentitySelectionService.Instance.SelectedInterns.FirstOrDefault();
            if (identity == null)
            {
                UIManager.Instance.HideCommandsOne();
                return;
            }
            this.currentCommandIdentity = identity;

            if (currentCommandIdentity.InternAI != null)
            {
                CameraFocusUI.Instance.FocusOnIntern(currentCommandIdentity.InternAI.Npc.transform);
            }
        }

        void Start()
        {
            if (TitleUI == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("No TextMeshProUGUI TitleUI found while loading CommandsAllController !");
            }
            SetTitleUIText(currentCommandIdentity.Name);

            if (ModNamePanelDescription == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("No TextMeshProUGUI ModNamePanelDescription found while loading CommandsAllController !");
            }
            SetModDescriptionText($"{PluginRuntimeProvider.Context.Plugin_Name} v{PluginRuntimeProvider.Context.Plugin_Version}");

            // List of command buttons
            CommandButtons = GetComponentsInChildren<CommandButtonController>();
            if (CommandButtons == null
                || CommandButtons.Length == 0)
            {
                PluginLoggerHook.LogWarning?.Invoke("No CommandButtons found while loading CommandsAllController !");
            }
        }

        private IEnumerator UpdateCommandsUI()
        {
            yield return null;

            while (this.enabled)
            {
                // Buttons
                CommandButtonController? commandWheelController = GetGoToVehicleButton();
                if (commandWheelController != null)
                {
                    commandWheelController.IsNotAvailable = InternManager.Instance.VehicleController == null;
                }

                yield return null;
            }
        }

        private CommandButtonController? GetGoToVehicleButton()
        {
            if (CommandButtons != null)
            {
                return CommandButtons.FirstOrDefault(x => x.TypeInputAction == EnumInputAction.GoToVehicle);
            }
            return null;
        }

        private void SetModNamePanelDescriptionFont(TMP_FontAsset font)
        {
            if (ModNamePanelDescription != null)
            {
                ModNamePanelDescription.font = font;
            }
        }
        private void SetTitleUIFont(TMP_FontAsset font)
        {
            if (TitleUI != null)
            {
                TitleUI.font = font;
            }
        }

        private void SetTitleUIText(string text)
        {
            if (TitleUI != null)
            {
                TitleUI.text = text;
            }
        }
        private void SetModDescriptionText(string text)
        {
            if (ModNamePanelDescription != null)
            {
                ModNamePanelDescription.text = text;
            }
        }
    }
}
