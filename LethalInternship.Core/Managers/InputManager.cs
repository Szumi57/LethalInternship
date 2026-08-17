using GameNetcodeStuff;
using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.CommandsSystem.Abilities;
using LethalInternship.Core.UI.CommandsControllers;
using LethalInternship.Core.UI.CommandsControllers.DualSwitch;
using LethalInternship.Core.UI.CommandsControllers.GatheringPoint;
using LethalInternship.Core.UI.CommandsControllers.Suits;
using LethalInternship.Core.UI.InternBlocks;
using LethalInternship.Core.UI.ItemBlocks;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.CommandsSystem;
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

        public TargetedAbility? CurrentTargetedAbility { get; private set; }
        public TargetedAbility? PreviousTargetedAbility { get; private set; }

        private InputActionAsset inputActionAsset = null!;
        private Dictionary<InputAction, GameAction> actionMap = new Dictionary<InputAction, GameAction>();

        private LineRendererUtil LineRendererUtil = null!;

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
            PluginLoggerHook.LogInfo?.Invoke("InputManager loading input events...");

            PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.performed += Manage_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern.performed += GiveItemToIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsOneIntern.performed += OpenCommandsOneIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern.performed += OpenAllCommandsIntern_performed;

            CommandButtonController.OnSelected += CommandButtonController_OnSelected;
            ButtonDualSwitchParentController.OnDualSwitchSelected += CommandButtonController_OnSelected;
            InternBlockUI.OnSelected += InternBlockUI_OnSelected;
            ItemButtonController.OnSelected += ItemButtonController_OnSelected;
            GatheringPointController.OnSelected += GatheringPoint_OnSelected;
            RemoveGatheringPointController.OnSelected += RemoveGatheringPoint_OnSelected;

            // Suits
            ButtonSuitsController.OnSelected += ButtonSuitsController_OnSuitSelected;
            ButtonSelectSuit.OnSuitSelected += ButtonSelectSuit_OnSuitSelected;

            // BuildActionMap
            inputActionAsset = IngamePlayerSettings.Instance.playerInput.actions;
            foreach (var map in inputActionAsset.actionMaps)
            {
                foreach (InputAction? action in map.actions)
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
                foreach (InputAction? action in map.actions)
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
            ButtonDualSwitchParentController.OnDualSwitchSelected -= CommandButtonController_OnSelected;
            InternBlockUI.OnSelected -= InternBlockUI_OnSelected;
            ItemButtonController.OnSelected -= ItemButtonController_OnSelected;
            GatheringPointController.OnSelected -= GatheringPoint_OnSelected;
            RemoveGatheringPointController.OnSelected -= RemoveGatheringPoint_OnSelected;

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
            if (CurrentTargetedAbility != null)
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
        }

        private void LateUpdate()
        {
            PreviousTargetedAbility = null;
        }

        public void Init()
        {
            // Just to trigger lazy loading with Awake
            PluginLoggerHook.LogDebug?.Invoke("Initializing InputManager...");
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
            if (CurrentTargetedAbility == null)
                return;

            // Submitting action
            if (CurrentTargetedAbility.SubmitActions.Contains(gameAction))
            {
                TargetData? target = TargetingManager.Instance.GetCurrentTarget();
                if (Mouse.current.leftButton.wasPressedThisFrame
                    && target != null)
                {
                    Order? order = CurrentTargetedAbility.ResolveTarget(target.Value);
                    if (order != null)
                    {
                        InternManager.Instance.ExecuteOrder(order);
                        CancelTargeting();
                    }
                }
                return;
            }

            if (!CurrentTargetedAbility.NotInterruptingActions.Contains(gameAction))
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
            CurrentTargetedAbility = ability;
        }

        public void CancelTargeting()
        {
            if (CurrentTargetedAbility != null)
            {
                PreviousTargetedAbility = CurrentTargetedAbility;
                CurrentTargetedAbility = null;
                CommandContextService.Instance.ExitCommandMode();
                TargetingManager.Instance.SetActiveSearch(TargetingManager.TargetType.Intern);
                UIManager.Instance.HideInputIcon();
            }
        }

        #endregion

        #region Command intern

        private void CommandButtonController_OnSelected(EnumInputAction typeInputAction)
        {
            InputLock.BlockThisFrame();

            switch (typeInputAction)
            {
                // Direct orders
                case EnumInputAction.FollowMe:
                    new FollowMeAbility().Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.StayHere:
                    new StayHereAbility().Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.GoToShip:
                    new GoToShipAbility().Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.GoToVehicle:
                    new GoToVehicleAbility().Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;

                // Drop item
                case EnumInputAction.DropItem:
                    new DropHereAbility(dropAll: false).Activate();
                    break;
                case EnumInputAction.DropAllItems:
                    new DropHereAbility(dropAll: true).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.DropAllItemsInShip:
                    new DropToAbility(EnumCommandTypes.DropAllItemsToShip).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.DropAllItemsOnGatheringPoint:
                    new DropToAbility(EnumCommandTypes.DropAllItemsOnGatheringPoint).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.DropAllItemsInCruiser:
                    new DropToAbility(EnumCommandTypes.DropAllItemsInCruiser).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;

                // Unload
                case EnumInputAction.UnloadCruiser:
                    new UnloadFromAbility(EnumCommandTypes.UnloadCruiser).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.UnloadGatheringPoint:
                    new UnloadFromAbility(EnumCommandTypes.UnloadGatheringPoint).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;

                // Scavenge
                case EnumInputAction.ScavengeToShip:
                    new ScavengeToDropLocationAbility(EnumCommandTypes.ScavengingToShip).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.ScavengeToCruiser:
                    new ScavengeToDropLocationAbility(EnumCommandTypes.ScavengingToCruiser).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
                case EnumInputAction.ScavengeToGatheringPoint:
                    new ScavengeToDropLocationAbility(EnumCommandTypes.ScavengingToGatheringPoint).Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;

                // Update option
                case EnumInputAction.SetToAutoFlee:
                    new SetAutoDefenseAbility(autoDefense: false).Activate();
                    break;
                case EnumInputAction.SetToAutoDefense:
                    new SetAutoDefenseAbility(autoDefense: true).Activate();
                    break;

                // Context ability
                case EnumInputAction.PointToAction:
                    new ContextOrderAbility().Activate();
                    break;

                // UI
                case EnumInputAction.Close:
                    CommandContextService.Instance.ExitCommandMode();
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
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    UIManager.Instance.HideInputIcon();
                    break;
            }
        }

        private void ButtonSuitsController_OnSuitSelected(EnumInputAction typeInputAction)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            switch (typeInputAction)
            {
                case EnumInputAction.PreviousSuit:
                    new ChangeSuitAbility(ChangeSuitAbility.SuitSelectionMode.Previous).Activate();
                    break;
                case EnumInputAction.NextSuit:
                    new ChangeSuitAbility(ChangeSuitAbility.SuitSelectionMode.Next).Activate();
                    break;
                case EnumInputAction.SameSuit:
                    new ChangeSuitAbility(ChangeSuitAbility.SuitSelectionMode.Same, localPlayer.currentSuitID).Activate();
                    break;
                case EnumInputAction.RandomSuit:
                    new ChangeSuitAbility(ChangeSuitAbility.SuitSelectionMode.Random).Activate();
                    break;
            }
        }

        private void ButtonSelectSuit_OnSuitSelected(int suitID)
        {
            new ChangeSuitAbility(ChangeSuitAbility.SuitSelectionMode.Selected, suitID).Activate();
        }

        private void InternBlockUI_OnSelected()
        {
            UIManager.Instance.HideCommandsAll(resetCameraFocus: false);
            UIManager.Instance.ShowCommandsOne();
        }

        private void ItemButtonController_OnSelected(GrabbableObject grabbableObject, EnumInputAction typeInputAction)
        {
            IInternIdentity? identity = IdentitySelectionService.Instance.GetCurrent();
            if (identity == null
                || !identity.Alive
                || identity.InternAI == null)
            {
                return;
            }

            switch (typeInputAction)
            {
                case EnumInputAction.SwapWeapon:
                    identity.InternAI.BeginSwapWeaponWith(grabbableObject);
                    break;

                case EnumInputAction.ActivateItem:
                    identity.InternAI.UseItem(grabbableObject);
                    break;

                case EnumInputAction.DropItem:
                    identity.InternAI.DropItem(grabbableObject);
                    break;
            }
        }

        private void GatheringPoint_OnSelected(EnumInputAction typeInputAction)
        {
            switch (typeInputAction)
            {
                case EnumInputAction.SetGatheringPoint:
                    new SetGatheringPointAbility().Activate();
                    break;
                case EnumInputAction.GoToGatheringPoint:
                    new GoToGatheringPointAbility().Activate();
                    CommandContextService.Instance.ExitCommandMode();
                    UIManager.Instance.HideAll();
                    break;
            }
        }

        private void RemoveGatheringPoint_OnSelected()
        {
            InternManager.Instance.SetGatheringPoint(null);
        }

        #endregion

        #region Input action

        private void InputAction_ShowCommandsAll()
        {
            CancelTargeting();
            UIManager.Instance.HideCommandsOne();

            IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesSpawned());
            IdentitySelectionService.Instance.SelectAll();

            if (UIManager.Instance.IsCommandsAllOpened)
            {
                CommandContextService.Instance.ExitCommandMode();
                UIManager.Instance.HideCommandsAll();
            }
            else
            {
                CommandContextService.Instance.EnterCommandMode();
                UIManager.Instance.ShowCommandsAll();
            }
        }

        private void InputAction_NextIntern()
        {
            IInternIdentity? next = IdentitySelectionService.Instance
                                                .NextWhere(i => IdentityManager.Instance.IsIdentityValidToCommand(i));
            if (next != null)
            {
                IdentitySelectionService.Instance.SelectSingle(next);
                UIManager.Instance.RefreshCommandsOne();
            }
        }

        private void InputAction_PreviousIntern()
        {
            IInternIdentity? previous = IdentitySelectionService.Instance
                                                    .PreviousWhere(i => IdentityManager.Instance.IsIdentityValidToCommand(i));
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
            if (intern.NpcController.GetSqrDistanceWithLocalPlayer() > localPlayer.grabDistance * localPlayer.grabDistance)
                return;

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
            if (intern.NpcController.GetSqrDistanceWithLocalPlayer() > localPlayer.grabDistance * localPlayer.grabDistance)
                return;

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
                        GrabbableObject? itemToDrop = intern.ChooseFirstPickedUpItem(EnumOptionsGetItems.IgnoreWeapon);
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
            if (intern.NpcController.GetSqrDistanceWithLocalPlayer() > localPlayer.grabDistance * localPlayer.grabDistance)
                return;

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

            CancelTargeting();
            UIManager.Instance.HideCommandsAll(resetCameraFocus: false);

            IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesSpawned());
            IdentitySelectionService.Instance.SelectSingle(target.Value.Intern.InternIdentity);

            if (UIManager.Instance.IsCommandsOneOpened)
            {
                CommandContextService.Instance.ExitCommandMode();
                UIManager.Instance.HideCommandsOne();
            }
            else
            {
                CommandContextService.Instance.EnterCommandMode();
                UIManager.Instance.ShowCommandsOne();
            }
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
