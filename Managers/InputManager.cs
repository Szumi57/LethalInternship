using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Interfaces;
using LethalInternship.Interns.AI;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Utils;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace LethalInternship.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; } = null!;

        public EnumInputAction CurrentInputAction;

        private bool openCommandsInternInputIsPressed;
        private InternAI currentCommandedIntern = null!;
        private LineRendererUtil LineRendererUtil = null!;

        private Coroutine? scanPositionCoroutine;
        private Vector3? lastNavMeshHitPoint = null;
        private bool isValidNavMeshPoint;

        private void Awake()
        {
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
                    UIManager.Instance.ShowInputIcon(isValidNavMeshPoint);
                    break;

                case EnumInputAction.None:
                default:
                    StopScanPositionCoroutine();
                    break;
            }

            CheckOpenCommandsInput();
        }

        private void AddEventHandlers()
        {
            Plugin.InputActionsInstance.ManageIntern.performed += Manage_performed;
            Plugin.InputActionsInstance.GiveTakeItem.performed += GiveTakeItem_performed;
            Plugin.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            Plugin.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            Plugin.InputActionsInstance.ChangeSuitIntern.performed += ChangeSuitIntern_performed;
        }

        public void RemoveEventHandlers()
        {
            Plugin.InputActionsInstance.ManageIntern.performed -= Manage_performed;
            Plugin.InputActionsInstance.GiveTakeItem.performed -= GiveTakeItem_performed;
            Plugin.InputActionsInstance.GrabIntern.performed -= GrabIntern_performed;
            Plugin.InputActionsInstance.ReleaseInterns.performed -= ReleaseInterns_performed;
            Plugin.InputActionsInstance.ChangeSuitIntern.performed -= ChangeSuitIntern_performed;
        }

        public void SetCurrentInputAction(EnumInputAction action)
        {
            CurrentInputAction = action;
        }

        #region Tips display

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

        public void AddInternsControlTip(HUDManager hudManager)
        {
            int index = -1;
            for (int i = 0; i < hudManager.controlTipLines.Length - 1; i++)
            {
                TextMeshProUGUI textMeshProUGUI = hudManager.controlTipLines[i + 1];
                if (textMeshProUGUI != null && textMeshProUGUI.enabled && string.IsNullOrWhiteSpace(textMeshProUGUI.text))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                index = hudManager.controlTipLines.Length - 1;
            }

            if (InternManager.Instance.IsLocalPlayerHoldingInterns())
            {
                WriteControlTipLine(hudManager.controlTipLines[index], Const.TOOLTIP_RELEASE_INTERNS, GetKeyAction(Plugin.InputActionsInstance.ReleaseInterns));
            }
            if (InternManager.Instance.IsLocalPlayerNextToChillInterns())
            {
                WriteControlTipLine(hudManager.controlTipLines[index], Const.TOOLTIP_MAKE_INTERN_LOOK, GetKeyAction(Plugin.InputActionsInstance.MakeInternLookAtPosition));
            }
        }

        private void WriteControlTipLine(TextMeshProUGUI line, string textToAdd, string keyAction)
        {
            if (!IsStringPresent(line.text, textToAdd))
            {
                if (!string.IsNullOrWhiteSpace(line.text))
                {
                    line.text += "\n";
                }
                line.text += string.Format(textToAdd, keyAction);
            }
        }

        private bool IsStringPresent(string stringCurrent, string stringToAdd)
        {
            string[] splits = stringCurrent.Split(new string[] { "[", "]\n" }, System.StringSplitOptions.None);
            foreach (string split in splits)
            {
                if (string.IsNullOrWhiteSpace(split))
                {
                    continue;
                }

                if (stringToAdd.Contains(split.Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        public void UpdateControlTip()
        {
            string[] currentControlTipLines = { };
            if (HUDManager.Instance.controlTipLines != null
                && HUDManager.Instance.controlTipLines.Length > 0)
            {
                currentControlTipLines = HUDManager.Instance.controlTipLines.Select(i => i.text).ToArray();
            }

            HUDManager.Instance.ChangeControlTipMultiple(currentControlTipLines);
        }

        #endregion

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
                if (localPlayer.hoveringOverTrigger.holdInteraction
                    || !PlayerControllerBPatch.InteractTriggerUseConditionsMet_ReversePatch(localPlayer))
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

            IPointOfInterest? location;
            switch (CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:
                    location = GetDefaultPoint();
                    if (location == null)
                    {
                        break;
                    }

                    // Current intern
                    currentCommandedIntern.SetCommandToGoToPosition(location);

                    CurrentInputAction = EnumInputAction.None;
                    break;

                case EnumInputAction.SendingAllInternsToLocation:
                    location = GetDefaultPoint();
                    if (location == null)
                    {
                        break;
                    }

                    // All owned interns (later close interns)
                    InternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                    foreach (InternAI intern in internsOwned)
                    {
                        intern.SetCommandToGoToPosition(location);
                    }

                    CurrentInputAction = EnumInputAction.None;
                    break;

                case EnumInputAction.None:
                default:
                    ManageIntern();
                    break;
            }
        }

        private IPointOfInterest? GetDefaultPoint()
        {
            Vector3? point;

            point = UIManager.Instance.GetWorldIconInCenter();
            if (point == null)
            {
                if (isValidNavMeshPoint
                && lastNavMeshHitPoint.HasValue)
                {
                    point = lastNavMeshHitPoint.Value;
                }
            }

            if (point != null)
            {
                InternManager.Instance.GetPointOfInterestOrDefaultInterestPoint(point.Value);
            }


            return null;

            //GameObject[] allAINodes;
            //if (lastNavMeshHitPoint.Value.y >= -80f)
            //{
            //    allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            //}
            //else
            //{
            //    allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            //}

            //return allAINodes.OrderBy(node => (node.transform.position - lastNavMeshHitPoint.Value).sqrMagnitude)
            //                 .FirstOrDefault()
            //                 .transform.position;
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
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                if (intern.OwnerClientId != localPlayer.actualClientId)
                {
                    intern.SyncAssignTargetAndSetMovingTo(localPlayer);

                    if (Plugin.Config.ChangeSuitAutoBehaviour.Value)
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
            if (!Plugin.InputActionsInstance.OpenCommandsIntern.IsPressed())
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
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
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
                    isValidNavMeshPoint = false;
                    yield return null;
                    continue;
                }

                Vector3? lastHitPoint = null;
                raycastHits = raycastHits.OrderBy(x => x.distance).ToArray();
                NavMeshHit hitMesh = new NavMeshHit();
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
                    UIManager.Instance.SetDefaultInputIcon();

                    if (NavMesh.SamplePosition(hit.point, out hitMesh, 5f, -1))
                    {
                        lastNavMeshHitPoint = hitMesh.position;
                    }

                    break;
                }

                if (lastHitPoint != null
                    && lastNavMeshHitPoint != null)
                {
                    isValidNavMeshPoint = (lastHitPoint.Value - lastNavMeshHitPoint.Value).sqrMagnitude < 2f * 2f;
                }
                else
                {
                    isValidNavMeshPoint = false;
                }

                yield return null;
            }

            isValidNavMeshPoint = false;
            yield break;
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
                InternAI? intern = InternManager.Instance.GetInternAI((int)internController.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                // To cut Discard_performed from triggering after this input
                AccessTools.Field(typeof(PlayerControllerB), "timeSinceSwitchingSlots").SetValue(localPlayer, 0f);

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
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null
                    || intern.IsSpawningAnimationRunning())
                {
                    continue;
                }

                intern.SyncAssignTargetAndSetMovingTo(localPlayer);
                // Grab intern
                intern.GrabInternServerRpc(localPlayer.playerClientId);

                UpdateControlTip();
                return;
            }
        }

        private void ReleaseInterns_performed(InputAction.CallbackContext obj)
        {
            // Profiler, does not concern intern logic
            if (Plugin.IsModMonoProfilerLoaderLoaded)
            {
                DumpMonoProfilerFile();
            }
            // ---------------------------------------

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // No intern in interact range
            // Check if we hold interns
            InternAI[] internsAIsHoldByPlayer = InternManager.Instance.GetInternsAiHoldByPlayer((int)localPlayer.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internsAIsHoldByPlayer[i].SyncReleaseIntern(localPlayer);
                }
            }

            HUDManager.Instance.ClearControlTips();
        }

        private void DumpMonoProfilerFile()
        {
            try
            {
                FileInfo dumpFile = MonoProfiler.MonoProfilerPatcher.RunProfilerDump();
                Plugin.LogDebug("-----------------------Saved profiler dump to " + dumpFile.FullName);
            }
            catch
            {
                Plugin.LogDebug("Could not dump profiler file. Ignore if not wanted.");
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
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
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
