using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
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
        public TextMeshProUGUI TitleUI = null!;
        public TextMeshProUGUI ModNamePanelDescription = null!;

        // Panels
        public GameObject QuickCommandsPanelUI = null!;
        public GameObject PointerCommandsPanelUI = null!;
        public GameObject AutoDefenseCommandsPanelUI = null!;
        public GameObject CarryBehaviourCommandsPanelUI = null!;
        public GameObject GotoCommandsPanelUI = null!;
        public GameObject ScavengeCommandsPanelUI = null!;

        private Coroutine CoroutineUpdateCommandsUI = null!;

        private IInternIdentity currentIdentity = null!;

        private IRefreshableUI[] refreshables = null!;
        private IVisibilityUI[] visibilityUIs = null!;

        void Awake()
        {
            if (TitleUI == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("No TextMeshProUGUI TitleUI found while loading CommandsAllController !");
            }

            if (ModNamePanelDescription == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("No TextMeshProUGUI ModNamePanelDescription found while loading CommandsAllController !");
            }

            refreshables = GetComponentsInChildren<IRefreshableUI>(includeInactive: true);
            visibilityUIs = GetComponentsInChildren<IVisibilityUI>(includeInactive: true);
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            IInternIdentity? identity = IdentitySelectionService.Instance.GetCurrent();
            if (identity == null)
            {
                UIManager.Instance.HideCommandsOne();
                return;
            }
            this.currentIdentity = identity;

            // Camera focus
            if (currentIdentity.InternAI != null)
            {
                CameraFocusUI.Instance.FocusOnIntern(currentIdentity.InternAI.Npc.transform);
            }

            // Update UI
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;

            SetTitleUIFont(fontToUse);
            SetTitleUIText(currentIdentity.Name);

            SetModNamePanelDescriptionFont(fontToUse);
            SetModDescriptionText($"{PluginRuntimeProvider.Context.Plugin_Name} v{PluginRuntimeProvider.Context.Plugin_Version}");

            // Update commands UI while displaying
            if (CoroutineUpdateCommandsUI != null)
            {
                StopCoroutine(CoroutineUpdateCommandsUI);
            }
            CoroutineUpdateCommandsUI = StartCoroutine(UpdateCommandsUI());

            // Refresh UI
            foreach (var refreshable in refreshables)
                refreshable.Refresh();
        }

        private IEnumerator UpdateCommandsUI()
        {
            if (StartOfRound.Instance == null)
                yield break;

            StartOfRound instanceSOR = StartOfRound.Instance;

            while (this.enabled)
            {
                if (DebugConst.ALLOW_COMMANDS_ALWAYS)
                {
                    foreach (var uiElement in visibilityUIs)
                    {
                        uiElement.SetInteractable(interactable: true);
                    }
                    yield return null;
                    continue;
                }

                // Managing interns ?
                bool managingIntern = IdentityManager.Instance.IsIdentityValidToCommand(currentIdentity);
                if (managingIntern)
                {
                    // Clean all
                    foreach (var uiElement in visibilityUIs)
                    {
                        uiElement.SetInteractable(interactable: true);
                    }

                    // Vehicle ?
                    foreach (var uiElement in visibilityUIs)
                    {
                        if (uiElement.GroupUI == EnumUIGroups.VehicleGroupButtons)
                        {
                            uiElement.SetInteractable(interactable: InternManager.Instance.VehicleController != null, UIConst.TOOLTIPBAR_NO_CRUISER);
                        }
                    }

                    // In space or on company building moon
                    if (instanceSOR.inShipPhase
                        || instanceSOR.shipIsLeaving
                        || InternManager.Instance.IsCurrentMoonCompanyMoon())
                    {
                        // Disable all but
                        foreach (var uiElement in visibilityUIs.Where(x => x.GroupUI != EnumUIGroups.ItemsList
                                                                        && x.GroupUI != EnumUIGroups.SuitMenu
                                                                        && x.GroupUI != EnumUIGroups.AutoDefenseButton
                                                                        && x.GroupUI != EnumUIGroups.NavigationGroupButtons
                                                                        && x.GroupUI != EnumUIGroups.CarryItemBehaviourButton))
                        {
                            uiElement.SetInteractable(interactable: false, UIConst.TOOLTIPBAR_NOT_IN_SPACE);
                        }
                    }
                }
                else
                {
                    // Not managing interns
                    // Disable all
                    foreach (var uiElement in visibilityUIs)
                    {
                        uiElement.SetInteractable(interactable: false, UIConst.TOOLTIPBAR_NO_INTERNS_TO_MANAGE);
                    }
                }

                yield return null;
            }
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
