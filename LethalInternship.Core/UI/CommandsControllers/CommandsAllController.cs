using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandsAllController : MonoBehaviour
    {
        // Panels
        public Image BGPanelUI = null!;
        public GameObject QuickCommandsPanelUI = null!;
        public GameObject PointerCommandsPanelUI = null!;
        public GameObject AutoDefenseCommandsPanelUI = null!;
        public GameObject CarryBehaviourCommandsPanelUI = null!;
        public GameObject GotoCommandsPanelUI = null!;
        public GameObject ScavengeCommandsPanelUI = null!;

        public TextMeshProUGUI TitleUI = null!;
        public TextMeshProUGUI ModNamePanelDescription = null!;

        private Coroutine CoroutineUpdateCommandsUI = null!;

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
            visibilityUIs = GetComponentsInChildren<IVisibilityUI>(includeInactive: true);
        }

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;

            SetTitleUIFont(fontToUse);
            SetTitleUIText(UIConst.UI_TITLE_COMMANDS_ALL);

            SetModNamePanelDescriptionFont(fontToUse);
            SetModDescriptionText($"{PluginRuntimeProvider.Context.Plugin_Name} v{PluginRuntimeProvider.Context.Plugin_Version}");

            // Update commands UI while displaying
            if (CoroutineUpdateCommandsUI != null)
            {
                StopCoroutine(CoroutineUpdateCommandsUI);
            }
            CoroutineUpdateCommandsUI = StartCoroutine(UpdateCommandsUI());

            SetAllVisible();
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
                bool managingInterns = IdentitySelectionService.Instance.GetSelected()
                                            .Where(x => IdentityManager.Instance.IsIdentityValidToCommand(x))
                                            .Any();
                if (managingInterns)
                {
                    // Clean all
                    foreach (var uiElement in visibilityUIs)
                    {
                        uiElement.SetInteractable(interactable: true);
                    }

                    foreach (var uiElement in visibilityUIs)
                    {
                        // Vehicle ?
                        if (uiElement.GroupUI == EnumUIGroups.VehicleGroupButtons)
                            uiElement.SetInteractable(interactable: InternManager.Instance.VehicleController != null, UIConst.TOOLTIPBAR_NO_CRUISER);
                        // Gathering point ?
                        if (uiElement.GroupUI == EnumUIGroups.GatheringPointGroupButtons)
                            uiElement.SetInteractable(interactable: InternManager.Instance.GatheringPoint != null, UIConst.TOOLTIPBAR_NO_CRUISER);
                    }

                    // In space or on company building moon
                    if (instanceSOR.inShipPhase
                        || instanceSOR.shipIsLeaving
                        || InternManager.Instance.IsCurrentMoonCompanyMoon())
                    {
                        // Disable all but
                        foreach (var uiElement in visibilityUIs.Where(x => x.GroupUI != EnumUIGroups.InternsList
                                                                        && x.GroupUI != EnumUIGroups.SuitMenu
                                                                        && x.GroupUI != EnumUIGroups.AutoDefenseButton
                                                                        && x.GroupUI != EnumUIGroups.NavigationGroupButtons
                                                                        && x.GroupUI != EnumUIGroups.Other
                                                                        && x.GroupUI != EnumUIGroups.CarryItemBehaviourButton))
                        {
                            uiElement.SetInteractable(interactable: false, UIConst.TOOLTIPBAR_NOT_IN_SPACE);
                        }
                    }
                }
                else // Not managing interns
                {
                    // Disable all
                    foreach (var uiElement in visibilityUIs.Where(x => x.GroupUI != EnumUIGroups.Other))
                    {
                        uiElement.SetInteractable(interactable: false, UIConst.TOOLTIPBAR_NO_INTERNS_TO_MANAGE);
                    }
                }

                yield return null;
            }
        }

        public void SetAllVisible()
        {
            SetUIElementVisible(BGPanelUI, visible: true);
            SetUIElementVisible(visibilityUIs.Select(x => x.Go), visible: true);
            SetUIElementVisible(QuickCommandsPanelUI, visible: true);
            SetUIElementVisible(PointerCommandsPanelUI, visible: true);
            SetUIElementVisible(AutoDefenseCommandsPanelUI, visible: true);
            SetUIElementVisible(CarryBehaviourCommandsPanelUI, visible: true);
            SetUIElementVisible(GotoCommandsPanelUI, visible: true);
            SetUIElementVisible(ScavengeCommandsPanelUI, visible: true);
        }

        public void SetOnlyListInternsAndSuitCommandsVisible()
        {
            SetUIElementVisible(visibilityUIs.Where(x => x.GroupUI == EnumUIGroups.SuitMenu).Select(x => x.Go), visible: true);

            SetUIElementVisible(visibilityUIs.Where(x => x.GroupUI != EnumUIGroups.InternsList
                                                      && x.GroupUI != EnumUIGroups.SuitMenu).Select(x => x.Go), visible: false);
            SetUIElementVisible(BGPanelUI, visible: false);
            SetUIElementVisible(QuickCommandsPanelUI, visible: false);
            SetUIElementVisible(PointerCommandsPanelUI, visible: false);
            SetUIElementVisible(AutoDefenseCommandsPanelUI, visible: false);
            SetUIElementVisible(CarryBehaviourCommandsPanelUI, visible: false);
            SetUIElementVisible(GotoCommandsPanelUI, visible: false);
            SetUIElementVisible(ScavengeCommandsPanelUI, visible: false);
        }

        private void SetUIElementVisible(IEnumerable<GameObject> list, bool visible)
        {
            foreach (var go in list) SetUIElementVisible(go, visible);
        }

        private void SetUIElementVisible(GameObject go, bool visible)
        {
            if (go != null)
                go.SetActive(visible);
        }

        private void SetUIElementVisible(Image img, bool visible)
        {
            if (img != null)
                img.enabled = visible;
        }
    }
}
