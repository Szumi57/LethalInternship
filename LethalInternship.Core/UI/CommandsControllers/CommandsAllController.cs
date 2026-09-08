using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.CommandsControllers.Suits;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

        public GameObject ControllerFirstElement = null!;

        public TextMeshProUGUI TitleUI = null!;
        public TextMeshProUGUI UIInputDescription = null!;
        public TextMeshProUGUI ModNamePanelDescription = null!;

        public ButtonSelectSuit ButtonSelectSuit = null!;

        private Coroutine CoroutineUpdateCommandsUI = null!;

        private IVisibilityUI[] visibilityUIs = null!;

        private InputAction inputActionInteract = null!;
        private InputAction inputActionDiscard = null!;
        private InputAction inputActionInspectItem = null!;
        private InputAction inputActionPingScan = null!;

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

            // Get interact inputAction
            InputActionAsset inputActionAsset = IngamePlayerSettings.Instance.playerInput.actions;
            foreach (var map in inputActionAsset.actionMaps)
            {
                foreach (InputAction? action in map.actions)
                {
                    switch (action.name)
                    {
                        case "Interact":
                            inputActionInteract = action;
                            break;
                        case "Discard":
                            inputActionDiscard = action;
                            break;
                        case "InspectItem":
                            inputActionInspectItem = action;
                            break;
                        case "PingScan":
                            inputActionPingScan = action;
                            break;
                    }
                }
            }
        }

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;

            SetTitleUIFont(fontToUse);
            SetTitleUIText(UIConst.UI_TITLE_COMMANDS_ALL);

            SetUIInputDescriptionFont(fontToUse);

            SetModNamePanelDescriptionFont(fontToUse);
            SetModDescriptionText($"{PluginRuntimeProvider.Context.Plugin_Name} v{PluginRuntimeProvider.Context.Plugin_Version}");

            // Update commands UI while displaying
            if (CoroutineUpdateCommandsUI != null)
            {
                StopCoroutine(CoroutineUpdateCommandsUI);
            }
            UIManager.Instance.UpdateLastSelectedUI(null);
            EventSystem.current.SetSelectedGameObject(null);

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
        private void SetUIInputDescriptionFont(TMP_FontAsset font)
        {
            if (UIInputDescription != null)
            {
                UIInputDescription.font = font;
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

            yield return null;

            StartOfRound sor = StartOfRound.Instance;

            bool? previousManagingInterns = null;
            bool? previousVehicleAvailable = null;
            bool? previousGatheringPointSet = null;
            bool? previousRestrictedLocation = null;
            bool? previousController = null;

            GameObject? previousSelected = null;
            bool wasOnSuitMenu = false;

            while (enabled)
            {
                bool managingInterns = IdentitySelectionService.Instance.GetSelected()
                                        .Any(x => IdentityManager.Instance.IsIdentityValidToCommand(x));

                bool vehicleAvailable = InternManager.Instance.VehicleController != null;
                bool gatheringPointSet = InternManager.Instance.GatheringPoint != null;

                bool restrictedLocation = sor.inShipPhase
                                            || sor.shipIsLeaving
                                            || InternManager.Instance.IsCurrentMoonCompanyMoon();

                bool usingController = InputManager.Instance.IsUsingController;
                CheckActiveSelectedUI(usingController);

                bool stateChanged = managingInterns != previousManagingInterns
                                || vehicleAvailable != previousVehicleAvailable
                                || gatheringPointSet != previousGatheringPointSet
                                || restrictedLocation != previousRestrictedLocation
                                || usingController != previousController
                                || EventSystem.current.currentSelectedGameObject == null;

                if (stateChanged)
                {
                    previousManagingInterns = managingInterns;
                    previousVehicleAvailable = vehicleAvailable;
                    previousGatheringPointSet = gatheringPointSet;
                    previousRestrictedLocation = restrictedLocation;
                    previousController = usingController;

                    UpdateInteractability(
                        managingInterns,
                        vehicleAvailable,
                        gatheringPointSet,
                        restrictedLocation
                    );

                    UpdateUIMessage(usingController);
                    UpdateActiveSelectedUI(usingController);
                }

                if (usingController)
                {
                    GameObject? selected = EventSystem.current.currentSelectedGameObject;
                    if (selected != previousSelected)
                    {
                        previousSelected = selected;

                        if (selected != null)
                        {
                            IGroupUI? groupUI = selected.GetComponent<IGroupUI>();
                            bool isOnSuitMenu = groupUI?.GroupUI == EnumUIGroups.SuitMenu
                                                || groupUI?.GroupUI == EnumUIGroups.InternsList;

                            if (isOnSuitMenu != wasOnSuitMenu)
                            {
                                wasOnSuitMenu = isOnSuitMenu;
                                if (isOnSuitMenu)
                                {
                                    UIManager.Instance.CommandsAllController.SetOnlyListInternsAndSuitCommandsVisible();
                                }
                                else
                                {
                                    UIManager.Instance.CommandsAllController.SetAllVisible();
                                    if (groupUI?.GroupUI != EnumUIGroups.AutoDefenseButton // Moving to AutoDefenseButton ok
                                        && ControllerFirstElement != null)
                                    {
                                        EventSystem.current.SetSelectedGameObject(ControllerFirstElement);
                                    }

                                    previousSelected = EventSystem.current.currentSelectedGameObject;
                                }
                            }
                        }
                    }
                }

                yield return null;
            }
        }

        private void UpdateInteractability(bool managingInterns,
                                           bool vehicleAvailable,
                                           bool gatheringPointSet,
                                           bool restrictedLocation)
        {
            if (DebugConst.ALLOW_COMMANDS_ALWAYS)
            {
                foreach (var ui in visibilityUIs)
                    ui.SetInteractable(true);

                return;
            }

            if (!managingInterns)
            {
                foreach (var ui in visibilityUIs)
                {
                    if (ui.GroupUI != EnumUIGroups.Other
                        && ui.GroupUI != EnumUIGroups.Close)
                        ui.SetInteractable(false, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_INTERNS_TO_MANAGE);
                }

                return;
            }

            foreach (var ui in visibilityUIs)
            {
                ui.SetInteractable(true);

                switch (ui.GroupUI)
                {
                    case EnumUIGroups.VehicleGroupButtons:
                        ui.SetInteractable(vehicleAvailable, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_CRUISER);
                        break;
                    case EnumUIGroups.GatheringPointGroupButtons:
                        ui.SetInteractable(gatheringPointSet, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_GATHERINGPOINT);
                        break;
                }

                if (restrictedLocation && !CanUseInRestrictedLocation(ui.GroupUI))
                {
                    ui.SetInteractable(false, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NOT_IN_SPACE);
                }
            }
        }

        private void UpdateUIMessage(bool usingController)
        {
            if (UIInputDescription == null)
                return;

            UIInputDescription.text = usingController ? string.Format(UIConst.UI_INPUT_MESSAGE_CONTROLLER,
                                                                      InputManager.Instance.GetKeyAction(inputActionInteract),
                                                                      InputManager.Instance.GetKeyAction(inputActionInspectItem),
                                                                      InputManager.Instance.GetKeyAction(inputActionPingScan),
                                                                      InputManager.Instance.GetKeyAction(inputActionDiscard))
                                                      : UIConst.UI_INPUT_MESSAGE_KEYBOARD;
        }

        private void CheckActiveSelectedUI(bool usingController)
        {
            if (usingController)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null)
                    return;

                if (!selected.transform.IsChildOf(this.gameObject.transform))
                {
                    // If we go out of the panel to the base game panel
                    // return to ScavengeToCruiser button
                    foreach (var commandButtonController in GetComponentsInChildren<CommandButtonController>())
                    {
                        if (commandButtonController != null
                            && commandButtonController.TypeInputAction == SharedAbstractions.Enums.EnumInputAction.ScavengeToCruiser)
                        {
                            EventSystem.current.SetSelectedGameObject(commandButtonController.gameObject);
                        }
                    }
                }
            }
        }

        private void UpdateActiveSelectedUI(bool usingController)
        {
            if (usingController)
            {
                if (UIManager.Instance.LastSelectedUI != null
                    && UIManager.Instance.LastSelectedUI.activeInHierarchy
                    && UIManager.Instance.LastSelectedUI.transform.IsChildOf(this.gameObject.transform))
                    EventSystem.current.SetSelectedGameObject(UIManager.Instance.LastSelectedUI);
                else if (ControllerFirstElement != null)
                    EventSystem.current.SetSelectedGameObject(ControllerFirstElement);
            }
        }

        private static bool CanUseInRestrictedLocation(EnumUIGroups group)
        {
            return group == EnumUIGroups.InternsList
                || group == EnumUIGroups.SuitMenu
                || group == EnumUIGroups.AutoDefenseButton
                || group == EnumUIGroups.NavigationGroupButtons
                || group == EnumUIGroups.Other
                || group == EnumUIGroups.Close
                || group == EnumUIGroups.CarryItemBehaviourButton;
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
            foreach (var ui in visibilityUIs)
            {
                if (ui.GroupUI == EnumUIGroups.SuitMenu
                    || ui.GroupUI == EnumUIGroups.AutoDefenseButton)
                {
                    SetUIElementVisible(ui.Go, visible: true);
                }
                else if (ui.GroupUI != EnumUIGroups.InternsList)
                {
                    SetUIElementVisible(ui.Go, visible: false);
                }
            }

            SetUIElementVisible(BGPanelUI, visible: false);
            SetUIElementVisible(QuickCommandsPanelUI, visible: false);
            SetUIElementVisible(PointerCommandsPanelUI, visible: false);
            SetUIElementVisible(AutoDefenseCommandsPanelUI, visible: true);
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
