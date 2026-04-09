using GameNetcodeStuff;
using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.CommandsSystem.Abilities;
using LethalInternship.Core.UI.CommandsControllers;
using LethalInternship.Core.UI.CommandsControllers.DualSwitch;
using LethalInternship.Core.UI.CommandsControllers.ItemBlocks;
using LethalInternship.Core.UI.CommandsControllers.Suits;
using LethalInternship.Core.UI.InternBlocks;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.MonoProfilerHooks;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalInternship.Core.Managers
{
    public class InputManager : MonoBehaviour, IInputManager
    {
        private static InputManager _instance = null!;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(InputManager));
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private InputActionAsset inputActionAsset = null!;
        private Dictionary<InputAction, GameAction> actionMap = new Dictionary<InputAction, GameAction>();

        private LineRendererUtil LineRendererUtil = null!;

        TargetedAbility? currentTargetedAbility;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            InputManagerProvider.Register(this);
        }

        public string GetKeyAction(InputAction inputAction)
        {
            int bindingIndex;
            if (StartOfRound.Instance.localPlayerUsingController)
            {
                // Gamepad
                bindingIndex = inputAction.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
            }
            else
            {
                // kbm
                bindingIndex = inputAction.GetBindingIndex(InputBinding.MaskByGroup("KeyboardAndMouse"));
            }
            return inputAction.GetBindingDisplayString(bindingIndex);
        }

        private void OnEnable()
        {
            PluginLoggerHook.LogInfo?.Invoke("Initializing InputManager...");

            PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.performed += Manage_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern.performed += GiveItemToIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsOneIntern.performed += OpenCommandsOneIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern.performed += OpenAllCommandsIntern_performed;

            CommandButtonController.OnSelected += CommandButtonController_OnSelected;
            ButtonDualSwitchParentController.OnDualSwitchSelected += DualSwitchController_OnSelected;
            InternBlockUI.OnSelected += InternBlockUI_OnSelected;
            ItemBlockUI.OnSelected += ItemBlockUI_OnSelected;

            // Suits
            ButtonSuitsController.OnSelected += ButtonSuitsController_OnSuitSelected;
            ButtonSelectSuit.OnSuitSelected += ButtonSelectSuit_OnSuitSelected;

            // BuildActionMap
            inputActionAsset = IngamePlayerSettings.Instance.playerInput.actions;
            foreach (var map in inputActionAsset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    switch (action.name)
                    {
                        case "Look": actionMap[action] = GameAction.Look; break;
                        case "Move": actionMap[action] = GameAction.Move; break;
                        case "Jump": actionMap[action] = GameAction.Jump; break;
                        case "Sprint": actionMap[action] = GameAction.Sprint; break;
                        case "Crouch": actionMap[action] = GameAction.Crouch; break;
                        case "Use": actionMap[action] = GameAction.Use; break;
                        case "ActivateItem": actionMap[action] = GameAction.ActivateItem; break;
                        case "SwitchItem": actionMap[action] = GameAction.SwitchItem; break;
                    }
                }
            }

            // SubscribeAllActions
            foreach (var map in inputActionAsset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    action.started += OnAnyAction;
                }
            }
        }

        private void OnDisable()
        {
            PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.performed -= Manage_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern.performed -= GiveItemToIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed -= GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed -= ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsOneIntern.performed -= OpenCommandsOneIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern.performed -= OpenAllCommandsIntern_performed;

#pragma warning disable CS8601 // Possible null reference assignment.
            CommandButtonController.OnSelected -= CommandButtonController_OnSelected;
            ButtonDualSwitchParentController.OnDualSwitchSelected -= DualSwitchController_OnSelected;
            InternBlockUI.OnSelected -= InternBlockUI_OnSelected;
            ItemBlockUI.OnSelected -= ItemBlockUI_OnSelected;

            ButtonSuitsController.OnSelected -= ButtonSuitsController_OnSuitSelected;
            ButtonSelectSuit.OnSuitSelected -= ButtonSelectSuit_OnSuitSelected;
#pragma warning restore CS8601 // Possible null reference assignment.

            // UnsubscribeAllActions
            foreach (var map in inputActionAsset.actionMaps)
                foreach (var action in map.actions)
                    action.started -= OnAnyAction;
        }

        private void OnDestroy()
        {
            InputManagerProvider.Unregister(this);
        }

        private void Update()
        {
            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            if (LineRendererUtil == null)
            {
                LineRendererUtil = new LineRendererUtil(1, GameNetworkManager.Instance.localPlayerController.transform);
            }

            // Commands system
            if (currentTargetedAbility != null)
            {
                // UI
                UIManager.Instance.HideAll();
                if (UIManager.Instance.GetPointOfInterestInCenter() != null)
                {
                    // Hide if another icon in center
                    UIManager.Instance.HideInputIcon();
                }
                else
                {
                    UIManager.Instance.ShowInputIcon();
                }
            }

            //switch (CurrentInputAction)
            //{
            //    case EnumInputAction.PointToAction:
            //        StartScanPositionCoroutine();
            //        UIManager.Instance.ShowInputIcon(isPointedValid);
            //        break;

            //    case EnumInputAction.FollowMe:
            //        GiveOrderFollowMe();
            //        SetCurrentInputAction(EnumInputAction.None);
            //        break;

            //    case EnumInputAction.GoToShip:
            //        GiveOrderGoToShip();
            //        SetCurrentInputAction(EnumInputAction.None);
            //        break;

            //    case EnumInputAction.GoToVehicle:
            //        GiveOrderGoToVehicle();
            //        SetCurrentInputAction(EnumInputAction.None);
            //        break;

            //    case EnumInputAction.ScavengeToShip:
            //        GiveOrderGoScavenging();
            //        SetCurrentInputAction(EnumInputAction.None);
            //        break;

            //    case EnumInputAction.None:
            //    default:
            //        StopScanPositionCoroutine();
            //        UIManager.Instance.HideInputIcon();
            //        break;
            //}
        }

        private bool IsPerformedValid(PlayerControllerB localPlayer)
        {
            if (!localPlayer.IsOwner
                || localPlayer.isPlayerDead
                || !localPlayer.isPlayerControlled)
            {
                return false;
            }

            if (localPlayer.isGrabbingObjectAnimation
                || localPlayer.isTypingChat
                || localPlayer.inTerminalMenu
                || localPlayer.IsInspectingItem)
            {
                return false;
            }
            if (localPlayer.inAnimationWithEnemy != null)
            {
                return false;
            }
            if (localPlayer.jetpackControls || localPlayer.disablingJetpackControls)
            {
                return false;
            }
            if (StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                return false;
            }

            if (localPlayer.hoveringOverTrigger != null)
            {
                if (localPlayer.hoveringOverTrigger.holdInteraction)
                {
                    return false;
                }

                bool interactTriggerUseConditionsMet = PlayerControllerBHook.InteractTriggerUseConditionsMet_ReversePatch?.Invoke(localPlayer) ?? false;
                if (!interactTriggerUseConditionsMet)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnAnyAction(InputAction.CallbackContext ctx)
        {
            if (!InputLock.CanProcessWorldInput)
                return;

            // Any action

            // Unknown action
            if (!actionMap.TryGetValue(ctx.action, out var gameAction))
            {
                UIManager.Instance.HideAll();
                CancelTargeting();
                return;
            }

            // Anything but
            if (gameAction != GameAction.Use
                && gameAction != GameAction.ActivateItem // Click
                && gameAction != GameAction.Look // Move mouse
                && gameAction != GameAction.SwitchItem) // Scroll
            {
                UIManager.Instance.HideAll();
            }

            // If waiting for a targeted ability
            // ---------------------------------
            if (currentTargetedAbility == null)
                return;

            // Submitting action
            if (currentTargetedAbility.SubmitActions.Contains(gameAction))
            {
                TargetData? target = TargetingManager.Instance.GetCurrentTarget();
                if (Mouse.current.leftButton.wasPressedThisFrame
                    && target != null)
                {
                    Order? order = currentTargetedAbility.ResolveTarget(target.Value);
                    if (order != null)
                    {
                        InternManager.Instance.ExecuteOrder(order);
                        CancelTargeting();
                    }
                }
                return;
            }

            if (!currentTargetedAbility.NotInterruptingActions.Contains(gameAction))
            {
                CancelTargeting();
                return;
            }
            // Not interrupting action
            // Do nothing
        }

        #region Commands System

        public void StartTargeting(TargetedAbility ability)
        {
            currentTargetedAbility = ability;
            TargetingManager.Instance.SetActiveSearch(TargetingManager.TargetType.Enemy | TargetingManager.TargetType.Item);
        }

        public void CancelTargeting()
        {
            currentTargetedAbility = null;
            TargetingManager.Instance.SetActiveSearch(TargetingManager.TargetType.Intern);
            UIManager.Instance.HideInputIcon();
        }

        #endregion

        #region Command intern

        private void CommandButtonController_OnSelected(EnumInputAction typeInputAction)
        {
            InputLock.BlockThisFrame();

            switch (typeInputAction)
            {
                case EnumInputAction.FollowMe:
                    new FollowMeAbility().Activate();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.PointToAction:
                    new ContextOrderAbility().Activate();
                    break;
                case EnumInputAction.GoToShip:
                    new GoToShipAbility().Activate();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.GoToVehicle:
                    new GoToVehicleAbility().Activate();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.ScavengeToShip:
                    new ScavengeToShipAbility().Activate();
                    UIManager.Instance.HideAll();
                    break;

                case EnumInputAction.Close:
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.ReturnToAll:
                    InputAction_ShowCommandsAll();
                    break;
                case EnumInputAction.NextIntern:
                    InputAction_NextIntern();
                    break;
                case EnumInputAction.PreviousIntern:
                    InputAction_PreviousIntern();
                    break;

                case EnumInputAction.None:
                default:
                    UIManager.Instance.HideAll();
                    UIManager.Instance.HideInputIcon();
                    break;
            }
        }

        private void DualSwitchController_OnSelected((EnumInputAction, EnumClickSide) args)
        {
            InputLock.BlockThisFrame();

            switch (args.Item1)
            {
                case EnumInputAction.SetToAutoFlee:
                    PluginLoggerHook.LogDebug?.Invoke($"DualSwitchController_OnSelected cliked auto flee");
                    break;
                case EnumInputAction.SetToAutoDefense:
                    PluginLoggerHook.LogDebug?.Invoke($"DualSwitchController_OnSelected cliked auto defense");
                    break;
            }
        }

        private void ButtonSuitsController_OnSuitSelected(EnumInputAction typeInputAction)
        {
            PluginLoggerHook.LogDebug?.Invoke($"ButtonSuitsController_OnSuitSelected {typeInputAction}");
        }

        private void ButtonSelectSuit_OnSuitSelected(int suitID)
        {
            PluginLoggerHook.LogDebug?.Invoke($"ButtonSelectSuit_OnSuitSelected {suitID} {StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName}");
        }

        private void InternBlockUI_OnSelected()
        {
            UIManager.Instance.HideCommandsAll(resetCameraFocus: false);
            UIManager.Instance.ShowCommandsOne();
        }

        private void ItemBlockUI_OnSelected(GrabbableObject grabbableObject)
        {
            IInternIdentity? identity = IdentitySelectionService.Instance.GetCurrent();
            if (identity == null
                || !identity.Alive
                || identity.InternAI == null)
            {
                return;
            }

            // Drop item
            identity.InternAI.DropItem(grabbableObject);
        }

        #endregion

        #region Input action

        private void InputAction_ShowCommandsAll()
        {
            UIManager.Instance.HideCommandsOne();

            IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesOwnedByLocal());
            IdentitySelectionService.Instance.SelectAll();
            UIManager.Instance.ToogleCommandsAll();
        }

        private void InputAction_NextIntern()
        {
            IInternIdentity? next = IdentitySelectionService.Instance
                                                .NextWhere(i => i.Alive
                                                             && i.InternAI != null
                                                             && i.InternAI.NpcController.GetSqrDistanceWithLocalPlayer() < UIConst.DISTANCE_UI_PROXIMITY * UIConst.DISTANCE_UI_PROXIMITY);
            if (next != null)
            {
                IdentitySelectionService.Instance.SelectSingle(next);
                UIManager.Instance.RefreshCommandsOne();
            }
        }

        private void InputAction_PreviousIntern()
        {
            IInternIdentity? previous = IdentitySelectionService.Instance
                                                    .PreviousWhere(i => i.Alive
                                                                     && i.InternAI != null
                                                                     && i.InternAI.NpcController.GetSqrDistanceWithLocalPlayer() < UIConst.DISTANCE_UI_PROXIMITY * UIConst.DISTANCE_UI_PROXIMITY);
            if (previous != null)
            {
                IdentitySelectionService.Instance.SelectSingle(previous);
                UIManager.Instance.RefreshCommandsOne();
            }
        }

        #endregion

        #region Shortcut performed

        private void Manage_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null
                || target.Value.Intern == null)
                return;

            IInternAI intern = target.Value.Intern;
            if (intern.OwnerClientId != localPlayer.actualClientId)
            {
                intern.SyncAssignTargetAndSetMovingTo(localPlayer);

                if (PluginRuntimeProvider.Context.Config.ChangeSuitAutoBehaviour)
                {
                    intern.ChangeSuitInternServerRpc(intern.Npc.playerClientId, localPlayer.currentSuitID);
                }
            }
        }

        private void GiveItemToIntern_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null
                || target.Value.Intern == null)
                return;

            IInternAI intern = target.Value.Intern;

            // To cut Discard_performed from triggering after this input
            FieldInfo fieldInfo = typeof(PlayerControllerB).GetField("timeSinceSwitchingSlots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fieldInfo.SetValue(localPlayer, 0f);

            // Player has an item to give
            if (localPlayer.currentlyHeldObjectServer != null)
            {
                if (!intern.CanHoldItem(localPlayer.currentlyHeldObjectServer))
                {
                    if (localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded && intern.IsHoldingTwoHandedItem())
                    {
                        intern.DropTwoHandItem();
                    }
                    else
                    {
                        GrabbableObject? itemToDrop = intern.ChooseFirstPickedUpItem(PluginRuntimeProvider.Context.Config.CanUseWeapons ? EnumOptionsGetItems.IgnoreWeapon : EnumOptionsGetItems.All);
                        if (itemToDrop != null)
                        {
                            intern.DropItem(itemToDrop);
                        }
                    }
                }

                // Intern take item from player hands
                intern.GiveItemToInternServerRpc(localPlayer.playerClientId, localPlayer.currentlyHeldObjectServer.NetworkObject);
            }
        }

        private void GrabIntern_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null
                || target.Value.Intern == null)
                return;

            IInternAI intern = target.Value.Intern;
            intern.SyncAssignTargetAndSetMovingTo(localPlayer);
            // Grab intern
            intern.GrabInternServerRpc(localPlayer.playerClientId);
        }

        private void ReleaseInterns_performed(InputAction.CallbackContext obj)
        {
            // Profiler, does not concern intern logic
            if (PluginRuntimeProvider.Context.IsModMonoProfilerLoaderLoaded)
            {
                MonoProfilerHook.DumpMonoProfilerFile?.Invoke();
            }
            // ---------------------------------------

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // No intern in interact range
            // Check if we hold interns
            IInternAI[] internsAIsHoldByPlayer = InternManager.Instance.GetInternsAiHoldByPlayer((int)localPlayer.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internsAIsHoldByPlayer[i].SyncReleaseIntern(localPlayer);
                }
            }
        }

        private void OpenAllCommandsIntern_performed(InputAction.CallbackContext obj)
        {
            InputLock.BlockThisFrame();

            InputAction_ShowCommandsAll();
        }

        private void OpenCommandsOneIntern_performed(InputAction.CallbackContext obj)
        {
            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null
                || target.Value.Intern == null)
                return;

            InputLock.BlockThisFrame();
            UIManager.Instance.HideCommandsAll(resetCameraFocus: false);

            IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesOwnedByLocal());
            IdentitySelectionService.Instance.SelectSingle(target.Value.Intern.InternIdentity);
            UIManager.Instance.ToogleCommandsOne();
        }

        #endregion

        public static class InputLock
        {
            static int blockedFrame = -1;

            public static bool CanProcessWorldInput =>
                Time.frameCount != blockedFrame;

            public static void BlockThisFrame()
            {
                blockedFrame = Time.frameCount;
            }
        }
    }
}
