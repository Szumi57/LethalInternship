using GameNetcodeStuff;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.MonoProfilerHooks;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace LethalInternship.Core.Managers
{
    public class InputManager : MonoBehaviour, IInputManager
    {
        public static InputManager Instance { get; private set; } = null!;

        public EnumInputAction CurrentInputAction;

        private bool openCommandsInternInputIsPressed;
        private IInternAI currentCommandedIntern = null!;
        private LineRendererUtil LineRendererUtil = null!;

        private Coroutine? scanPositionCoroutine;
        private Collider? lastColliderHit = null;
        private Vector3? lastNavMeshHitPoint = null;
        private bool isPointedValid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
            AddEventHandlers();
        }

        private void Start()
        {
            CurrentInputAction = EnumInputAction.None;
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

            switch (CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:
                case EnumInputAction.SendingAllInternsToLocation:
                    StartScanPositionCoroutine();
                    UIManager.Instance.ShowInputIcon(isPointedValid);
                    break;

                case EnumInputAction.None:
                default:
                    StopScanPositionCoroutine();
                    UIManager.Instance.HideInputIcon();
                    break;
            }

            // Hide if another icon in center
            if (UIManager.Instance.GetPointOfInterestInCenter() != null)
            {
                UIManager.Instance.HideInputIcon();
            }

            CheckOpenCommandsInput();
        }

        private void AddEventHandlers()
        {
            PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.performed += Manage_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GiveTakeItem.performed += GiveTakeItem_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ChangeSuitIntern.performed += ChangeSuitIntern_performed;
        }

        public void RemoveEventHandlers()
        {
            PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.performed -= Manage_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GiveTakeItem.performed -= GiveTakeItem_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.performed -= GrabIntern_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.performed -= ReleaseInterns_performed;
            PluginRuntimeProvider.Context.InputActionsInstance.ChangeSuitIntern.performed -= ChangeSuitIntern_performed;
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

        public void SetCurrentInputAction(EnumInputAction action)
        {
            CurrentInputAction = action;
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

        #region Command intern

        private void Manage_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

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
                         && lastNavMeshHitPoint.HasValue)
                {
                    pointOfInterest = InternManager.Instance.GetPointOfInterestOrDefaultInterestPoint(lastNavMeshHitPoint.Value);
                }
            }

            if (pointOfInterest == null)
            {
                return;
            }

            // Command intern(s)
            switch (CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:

                    // Current intern
                    currentCommandedIntern.SetCommandTo(pointOfInterest);
                    CurrentInputAction = EnumInputAction.None;
                    break;

                case EnumInputAction.SendingAllInternsToLocation:
                    // All owned interns (later close interns)
                    IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                    foreach (IInternAI intern in internsOwned)
                    {
                        intern.SetCommandTo(pointOfInterest);
                    }

                    CurrentInputAction = EnumInputAction.None;
                    break;

                case EnumInputAction.None:
                default:
                    ManageIntern();
                    break;
            }
        }

        private void ManageIntern()
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

        private void CheckOpenCommandsInput()
        {
            if (!PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsIntern.IsPressed())
            {
                openCommandsInternInputIsPressed = false;
                UIManager.Instance.HideCommands();
                return;
            }

            // If already open, do nothing
            if (openCommandsInternInputIsPressed)
            {
                return;
            }

            StopScanPositionCoroutine();
            openCommandsInternInputIsPressed = true;
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            // Check if pointing intern
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

                // Command single intern
                UIManager.Instance.ShowCommandsSingle();


                currentCommandedIntern = intern;
                return;
            }

            // Command all close interns
            UIManager.Instance.ShowCommandsMultiple();
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

            while (CurrentInputAction == EnumInputAction.SendingInternToLocation
                    || CurrentInputAction == EnumInputAction.SendingAllInternsToLocation)
            {
                // Scan 3D world
                Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
                RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, 100f, StartOfRound.Instance.walkableSurfacesMask);
                if (raycastHits.Length == 0)
                {
                    isPointedValid = false;
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
                    lastColliderHit = null;

                    // Pedestrian
                    UIManager.Instance.SetPedestrianInputIcon();

                    //PluginLoggerHook.LogDebug?.Invoke($"hit {hit.collider.gameObject.GetComponentInParent<VehicleController>()} trans : {hit.collider.gameObject.transform}, {hit.collider.gameObject.transform.parent?.transform}, {hit.collider.gameObject.transform.parent?.parent?.transform}");

                    NavMesh.CalculatePath(localPlayer.transform.position, hit.point, NavMesh.AllAreas, path);
                    if (path.status != NavMeshPathStatus.PathInvalid)
                    {
                        lastNavMeshHitPoint = hit.point;
                    }

                    if (lastHitPoint != null
                        && lastNavMeshHitPoint != null)
                    {
                        isPointedValid = (lastHitPoint.Value - lastNavMeshHitPoint.Value).sqrMagnitude < 2f * 2f;
                    }
                    else
                    {
                        isPointedValid = false;
                    }

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

        #endregion

        #region Give/Take Item

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

                if (!intern.AreHandsFree())
                {
                    // Intern drop item
                    intern.DropItem();
                }
                else if (localPlayer.currentlyHeldObjectServer != null)
                {
                    // Intern take item from player hands
                    GrabbableObject grabbableObject = localPlayer.currentlyHeldObjectServer;
                    intern.GiveItemToInternServerRpc(localPlayer.playerClientId, grabbableObject.NetworkObject);
                }

                return;
            }
        }

        #endregion

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

                UIManager.Instance.UpdateControlTip();
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

            HUDManager.Instance.ClearControlTips();
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

    }
}
