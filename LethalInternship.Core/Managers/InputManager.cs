using GameNetcodeStuff;
using LethalInternship.Core.CommandsSystem.Abilities;
using LethalInternship.Core.UI.CommandsControllers;
using LethalInternship.Core.UI.CommandsControllers.DualSwitch;
using LethalInternship.Core.UI.CommandsControllers.Suits;
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
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

        private EnumInputAction currentInputAction;
        public EnumInputAction CurrentInputAction { get => currentInputAction; }

        private IInternAI? currentCommandedIntern = null!;
        private LineRendererUtil LineRendererUtil = null!;

        private Coroutine? scanPositionCoroutine;
        private Collider? lastColliderHit = null;
        private Vector3? lastPointedHitPoint = null;
        private bool isPointedValid;

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
            PluginRuntimeProvider.Context.InputActionsInstance.GiveTakeItem.performed += GiveTakeItem_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ChangeSuitIntern.performed += ChangeSuitIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern.performed += OpenAllCommandsIntern_performed;

            CommandButtonController.OnSelected += CommandButtonController_OnSelected;
            ButtonDualSwitchParentController.OnDualSwitchSelected += DualSwitchController_OnSelected;

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
            PluginRuntimeProvider.Context.InputActionsInstance.GiveTakeItem.performed -= GiveTakeItem_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed -= GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed -= ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ChangeSuitIntern.performed -= ChangeSuitIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern.performed -= OpenAllCommandsIntern_performed;

#pragma warning disable CS8601 // Possible null reference assignment.
            CommandButtonController.OnSelected -= CommandButtonController_OnSelected;
            ButtonDualSwitchParentController.OnDualSwitchSelected -= DualSwitchController_OnSelected;

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
                UIManager.Instance.HideCommandsAll();
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
                UIManager.Instance.HideCommandsAll();
                CancelTargeting();
                return;
            }

            // Anything but
            if (gameAction != GameAction.Use
                && gameAction != GameAction.ActivateItem
                && gameAction != GameAction.Look)
            {
                UIManager.Instance.HideCommandsAll();
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
                        currentTargetedAbility = null;
                        UIManager.Instance.HideInputIcon();
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
        }

        public void CancelTargeting()
        {
            currentTargetedAbility = null;
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
                    break;

                case EnumInputAction.PointToAction:
                    new ContextOrderAbility().Activate();
                    break;


                case EnumInputAction.GoToShip:
                    GiveOrderGoToShip();
                    break;

                case EnumInputAction.GoToVehicle:
                    GiveOrderGoToVehicle();
                    break;

                case EnumInputAction.ScavengeToShip:
                    GiveOrderGoScavenging();
                    break;

                case EnumInputAction.Close:
                    UIManager.Instance.HideCommandsAll();
                    break;

                case EnumInputAction.None:
                default:
                    StopScanPositionCoroutine();
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

        private void GiveOrderFollowMe()
        {
            // Give order
            if (currentCommandedIntern == null)
            {
                // All owned interns (later close interns)
                IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                foreach (IInternAI intern in internsOwned)
                {
                    intern.SetCommandToFollowPlayer();
                }
            }
            else
            {
                // Current intern
                currentCommandedIntern.SetCommandToFollowPlayer();
            }
            SetCurrentInputAction(EnumInputAction.None);
        }

        private void GiveOrderGoToShip()
        {
            Transform? shipTransform = InternManager.Instance.ShipTransform;
            if (shipTransform == null)
            {
                PluginLoggerHook.LogError?.Invoke("InputManager GiveOrderGoToShip shipTransform not found !");
                return;
            }

            IPointOfInterest pointOfInterest = InternManager.Instance.GetPointOfInterestOrShipInterestPoint(shipTransform);
            // Give order
            if (currentCommandedIntern == null)
            {
                // All owned interns (later close interns)
                IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                foreach (IInternAI intern in internsOwned)
                {
                    intern.SetCommandTo(pointOfInterest);
                }
            }
            else
            {
                // Current intern
                currentCommandedIntern.SetCommandTo(pointOfInterest);
            }
        }

        private void GiveOrderGoToVehicle()
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("vehicleController not found !");
                return;
            }

            IPointOfInterest pointOfInterest = InternManager.Instance.GetPointOfInterestOrVehicleInterestPoint(vehicleController);
            // Give order
            if (currentCommandedIntern == null)
            {
                // All owned interns (later close interns)
                IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                foreach (IInternAI intern in internsOwned)
                {
                    intern.SetCommandTo(pointOfInterest);
                }
            }
            else
            {
                // Current intern
                currentCommandedIntern.SetCommandTo(pointOfInterest);
            }
        }

        private void GiveOrderGoScavenging()
        {
            // Give order
            if (currentCommandedIntern == null)
            {
                // All owned interns (later close interns)
                IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                foreach (IInternAI intern in internsOwned)
                {
                    intern.SetCommandToScavenging();
                }
            }
            else
            {
                // Current intern
                currentCommandedIntern.SetCommandToScavenging();
            }
        }

        #endregion

        public void SetCurrentInputAction(EnumInputAction action, IInternAI? internAIToCommand = null)
        {
            currentInputAction = action;
            this.currentCommandedIntern = internAIToCommand;
        }

        private void TryManageIntern()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            // Use of interact key to assign intern to player
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                IInternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                if (intern.OwnerClientId != localPlayer.actualClientId)
                {
                    intern.SyncAssignTargetAndSetMovingTo(localPlayer);

                    if (PluginRuntimeProvider.Context.Config.ChangeSuitAutoBehaviour)
                    {
                        intern.ChangeSuitInternServerRpc(player.playerClientId, localPlayer.currentSuitID);
                    }
                }

                //HUDManager.Instance.ClearControlTips();
                //HUDManager.Instance.ChangeControlTipMultiple(new string[] { Const.TOOLTIPS_ORDER_1 });
                return;
            }
        }

        private void StartScanPositionCoroutine()
        {
            if (scanPositionCoroutine == null)
            {
                scanPositionCoroutine = StartCoroutine(ScanPosition());
            }
        }

        private void StopScanPositionCoroutine()
        {
            if (scanPositionCoroutine != null)
            {
                StopCoroutine(scanPositionCoroutine);
                scanPositionCoroutine = null;
            }
        }

        private IEnumerator ScanPosition()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            while (CurrentInputAction == EnumInputAction.PointToAction)
            {
                isPointedValid = false;
                lastColliderHit = null;

                // Scan 3D world
                Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
                RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, 100f, StartOfRound.Instance.walkableSurfacesMask);
                if (raycastHits.Length == 0)
                {
                    //UIManager.Instance.SetPedestrianInputIcon();
                    yield return null;
                    continue;
                }

                Vector3? lastHitPoint = null;
                raycastHits = raycastHits.OrderBy(x => x.distance).ToArray();
                NavMeshPath path = new NavMeshPath();
                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 1f)
                    {
                        continue;
                    }

                    if (hit.collider.tag == "Player")
                    {
                        continue;
                    }

                    lastHitPoint = hit.point;

                    // Check for what we hit
                    if (IsColliderFromVehicle(hit.collider))
                    {
                        lastColliderHit = hit.collider;
                        isPointedValid = true;
                        UIManager.Instance.SetVehicleInputIcon();
                        break;
                    }
                    else if (IsColliderFromShip(hit.collider))
                    {
                        lastColliderHit = hit.collider;
                        isPointedValid = true;
                        UIManager.Instance.SetShipInputIcon();
                        break;
                    }
                    //PluginLoggerHook.LogDebug?.Invoke($"hit {hit.collider.gameObject.GetComponentInParent<VehicleController>()} trans : {hit.collider.gameObject.transform}, {hit.collider.gameObject.transform.parent?.transform}, {hit.collider.gameObject.transform.parent?.parent?.transform}");

                    // Pedestrian
                    UIManager.Instance.SetPositionInputIcon();
                    lastPointedHitPoint = hit.point;
                    isPointedValid = lastHitPoint != null;

                    break;
                }
                yield return null;
            }

            isPointedValid = false;
            yield break;
        }

        private bool IsColliderFromVehicle(Collider? collider)
        {
            return collider?.gameObject.GetComponentInParent<VehicleController>();
        }

        private bool IsColliderFromShip(Collider? collider)
        {
            return IsParentShip(collider?.gameObject.transform);
        }

        private bool IsParentShip(Transform? transform)
        {
            if (transform == null)
            {
                return false;
            }

            if (transform.name == "HangarShip")
            {
                return true;
            }

            return IsParentShip(transform.parent);
        }

        private Transform? GetParentShip(Transform? transform)
        {
            if (transform == null)
            {
                return null;
            }

            if (transform.name == "HangarShip")
            {
                return transform;
            }

            return GetParentShip(transform.parent);
        }


        #region Shortcut performed

        private void Manage_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Check if we are giving orders
            IPointOfInterest? pointOfInterest;

            // Get point in center
            pointOfInterest = UIManager.Instance.GetPointOfInterestInCenter();

            // No point of interest pointed
            if (pointOfInterest == null)
            {
                if (lastColliderHit != null && IsColliderFromVehicle(lastColliderHit))
                {
                    pointOfInterest = InternManager.Instance.GetPointOfInterestOrVehicleInterestPoint(lastColliderHit.gameObject.GetComponentInParent<VehicleController>());
                }
                else if (lastColliderHit != null && IsColliderFromShip(lastColliderHit))
                {
                    Transform? shipTransform = GetParentShip(lastColliderHit.gameObject.transform);
                    if (shipTransform != null)
                    {
                        pointOfInterest = InternManager.Instance.GetPointOfInterestOrShipInterestPoint(shipTransform);
                    }
                }
                else if (isPointedValid
                         && lastPointedHitPoint.HasValue)
                {
                    pointOfInterest = InternManager.Instance.GetPointOfInterestOrDefaultInterestPoint(lastPointedHitPoint.Value);
                }
            }
            isPointedValid = false;
            lastColliderHit = null;
            lastPointedHitPoint = null;

            // If still nothing, maybe try manage intern
            if (pointOfInterest == null)
            {
                if (CurrentInputAction == EnumInputAction.None)
                {
                    TryManageIntern();
                    return;
                }

                return;
            }

            // Give orders
            if (currentCommandedIntern == null)
            {
                // All owned interns (later close interns)
                IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                foreach (IInternAI intern in internsOwned)
                {
                    intern.SetCommandTo(pointOfInterest);
                }
            }
            else
            {
                // Current intern
                currentCommandedIntern.SetCommandTo(pointOfInterest);
            }
            SetCurrentInputAction(EnumInputAction.None);
        }

        private void GiveTakeItem_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Make an intern drop his object
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB internController = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (internController == null)
                {
                    continue;
                }
                IInternAI? intern = InternManager.Instance.GetInternAI((int)internController.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                // To cut Discard_performed from triggering after this input
                FieldInfo fieldInfo = typeof(PlayerControllerB).GetField("timeSinceSwitchingSlots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                fieldInfo.SetValue(localPlayer, 0f);

                // Player has no item to give
                if (localPlayer.currentlyHeldObjectServer == null)
                {
                    // Intern just drop item
                    GrabbableObject? itemToDrop = intern.ChooseLastPickedUpItem(EnumOptionsGetItems.ChooseWeaponLast);
                    if (itemToDrop != null)
                    {
                        intern.DropItem(itemToDrop);
                    }
                }
                else // Player has an item to give
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

                return;
            }
        }


        private void GrabIntern_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                IInternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                intern.SyncAssignTargetAndSetMovingTo(localPlayer);
                // Grab intern
                intern.GrabInternServerRpc(localPlayer.playerClientId);

                return;
            }
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

        private void ChangeSuitIntern_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Use of change suit key to change suit of intern
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                IInternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }


                if (intern.NpcController.Npc.currentSuitID == localPlayer.currentSuitID)
                {
                    intern.ChangeSuitInternServerRpc(intern.NpcController.Npc.playerClientId, 0);
                }
                else
                {
                    intern.ChangeSuitInternServerRpc(intern.NpcController.Npc.playerClientId, localPlayer.currentSuitID);
                }

                return;
            }
        }

        private void OpenAllCommandsIntern_performed(InputAction.CallbackContext obj)
        {
            InputLock.BlockThisFrame();

            UIManager.Instance.ToogleAllCommands();
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
