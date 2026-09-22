using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.CommandsControllers.Suits;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandsOneController : MonoBehaviour
    {
        public TextMeshProUGUI TitleUI = null!;
        public TextMeshProUGUI UIInputDescription = null!;
        public TextMeshProUGUI ModNamePanelDescription = null!;

        // Panels
        public GameObject QuickCommandsPanelUI = null!;
        public GameObject PointerCommandsPanelUI = null!;
        public GameObject AutoDefenseCommandsPanelUI = null!;
        public GameObject CarryBehaviourCommandsPanelUI = null!;
        public GameObject GotoCommandsPanelUI = null!;
        public GameObject ScavengeCommandsPanelUI = null!;

        public GameObject ControllerFirstElement = null!;

        public ButtonSelectSuit ButtonSelectSuit = null!;

        private Coroutine CoroutineUpdateCommandsUI = null!;

        private IInternIdentity currentIdentity = null!;

        private IRefreshableUI[] refreshables = null!;
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

            refreshables = GetComponentsInChildren<IRefreshableUI>(includeInactive: true);
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
            if (currentIdentity.InternAI != null
                && currentIdentity.InternAI.Npc != null)
            {
                CameraFocusUI.Instance.FocusOnIntern(currentIdentity.InternAI.Npc.transform);
            }

            // Update UI
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;

            SetTitleUIFont(fontToUse);
            SetTitleUIText(currentIdentity.Name);

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

            // Refresh UI
            foreach (var refreshable in refreshables)
                refreshable.Refresh();
        }

        private IEnumerator UpdateCommandsUI()
        {
            if (StartOfRound.Instance == null)
                yield break;

            yield return null;

            StartOfRound instanceSOR = StartOfRound.Instance;

            bool? previousManagingIntern = null;
            bool? previousVehicleAvailable = null;
            bool? previousGatheringPointSet = null;
            bool? previousRestrictedLocation = null;
            bool? previousController = null;

            while (enabled)
            {
                bool managingIntern = IdentityManager.Instance.IsIdentityValidToCommand(currentIdentity);

                bool vehicleAvailable = InternManager.Instance.VehicleController != null;
                bool gatheringPointSet = InternManager.Instance.GatheringPoint != null;

                bool restrictedLocation = instanceSOR.inShipPhase
                                || instanceSOR.shipIsLeaving
                                || InternManager.Instance.IsCurrentMoonCompanyMoon();

                bool usingController = InputManager.Instance.IsUsingController;
                CheckActiveSelectedUI(usingController);

                bool stateChanged = managingIntern != previousManagingIntern
                                    || vehicleAvailable != previousVehicleAvailable
                                    || gatheringPointSet != previousGatheringPointSet
                                    || restrictedLocation != previousRestrictedLocation
                                    || usingController != previousController
                                    || (usingController && EventSystem.current.currentSelectedGameObject == null);

                if (stateChanged)
                {
                    previousManagingIntern = managingIntern;
                    previousVehicleAvailable = vehicleAvailable;
                    previousGatheringPointSet = gatheringPointSet;
                    previousRestrictedLocation = restrictedLocation;
                    previousController = usingController;

                    UpdateCommandsInteractability(
                        managingIntern,
                        vehicleAvailable,
                        gatheringPointSet,
                        restrictedLocation
                    );

                    UpdateUIMessage(usingController);
                    UpdateActiveSelectedUI(usingController);
                }

                yield return null;
            }
        }

        private void UpdateCommandsInteractability(bool managingIntern,
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

            if (!managingIntern)
            {
                foreach (var uiElement in visibilityUIs)
                {
                    uiElement.SetInteractable(false, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_INTERNS_TO_MANAGE);
                }

                return;
            }

            foreach (var uiElement in visibilityUIs)
            {
                uiElement.SetInteractable(true);

                switch (uiElement.GroupUI)
                {
                    case EnumUIGroups.VehicleGroupButtons:
                        uiElement.SetInteractable(vehicleAvailable, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_CRUISER);
                        break;
                    case EnumUIGroups.GatheringPointGroupButtons:
                        uiElement.SetInteractable(gatheringPointSet, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NO_GATHERINGPOINT);
                        break;
                }

                if (restrictedLocation && !CanUseInRestrictedLocation(uiElement.GroupUI))
                {
                    uiElement.SetInteractable(false, tooltipMessageNotInteractable: UIConst.TOOLTIPBAR_NOT_IN_SPACE);
                }
            }
        }

        private static bool CanUseInRestrictedLocation(EnumUIGroups group)
        {
            return group == EnumUIGroups.ItemsList
                || group == EnumUIGroups.SuitMenu
                || group == EnumUIGroups.AutoDefenseButton
                || group == EnumUIGroups.NavigationGroupButtons
                || group == EnumUIGroups.Close
                || group == EnumUIGroups.CarryItemBehaviourButton;
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
        private void SetUIInputDescriptionFont(TMP_FontAsset font)
        {
            if (UIInputDescription != null)
            {
                UIInputDescription.font = font;
            }
        }
    }
}
