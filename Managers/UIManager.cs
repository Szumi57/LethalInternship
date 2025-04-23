using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Interns.AI.AIStates;
using LethalInternship.Interns.AI;
using LethalInternship.UI;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using LethalInternship.Constants;
using System.Data;

namespace LethalInternship.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;

        // Single Intern
        public GameObject CommandsSingleIntern = null!;
        public bool IsCommandsSingleInternOpened { get { return CommandsSingleIntern != null && CommandsSingleIntern.activeSelf; } }

        // Multiple Interns
        public GameObject CommandsMultipleInterns = null!;
        public bool IsCommandsMultipleInternsOpened { get { return CommandsMultipleInterns != null && CommandsMultipleInterns.activeSelf; } }

        // Canvas overlay
        public Canvas CanvasOverlay = null!;

        // Icon dispenser
        private IconUIDispenser iconUIDispenser = null!;

        private Coroutine? scanPositionCoroutine;

        private PlayerControllerB localPlayerController = null!;
        private bool InternsOwned;
        private Vector3? lastNavMeshHitPoint = null;
        private bool isValidNavMeshPoint;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
        }

        private void Update()
        {
            switch (InputManager.Instance.CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:

                    StartScanPositionCoroutine();

                    IconUI iconHereSimple = iconUIDispenser.GetHereSimpleIcon();
                    iconHereSimple.SetPositionUICenter();
                    iconHereSimple.SetColorIconValidOrNot(isValidNavMeshPoint);

                    break;

                case EnumInputAction.SendingAllInternsToLocation:

                    StartScanPositionCoroutine();

                    IconUI iconHereMultiple = iconUIDispenser.GetHereMultipleIcon();
                    iconHereMultiple.SetPositionUICenter();
                    iconHereMultiple.SetColorIconValidOrNot(isValidNavMeshPoint);

                    break;

                case EnumInputAction.None:
                default:
                    StopScanPositionCoroutine();
                    break;
            }

            // Show other active icons
            InternAI[] internAIsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
            InternsOwned = internAIsOwned.Length > 0;

            var internsByCommandPoints = internAIsOwned
                         .Where(y => y.CommandPoint != null)
                         .GroupBy(n => n.CommandPoint)
                         .Select(n => new
                         {
                             CommandPoint = n.Key,
                             numberInterns = n.Count()
                         });
            foreach (var commandPoint in internsByCommandPoints)
            {
                //Plugin.LogDebug($"commandPoint {commandPoint.CommandPoint} {commandPoint.numberInterns}");
                if (commandPoint.numberInterns == 1)
                {
                    IconUI iconHereSimple = iconUIDispenser.GetHereSimpleIcon();
#pragma warning disable CS8629 // Nullable value type may be null.
                    iconHereSimple.SetPositionUI(commandPoint.CommandPoint.Value);
                    iconHereSimple.SetDefaultColor();
                }
                else
                {
                    IconUI iconHereMultiple = iconUIDispenser.GetHereMultipleIcon();
                    iconHereMultiple.SetPositionUI(commandPoint.CommandPoint.Value);
#pragma warning restore CS8629 // Nullable value type may be null.
                    iconHereMultiple.SetDefaultColor();
                }
            }

            // Clear remaining icons
            iconUIDispenser.ClearDispenser();
        }

        private void LateUpdate()
        {
            switch (InputManager.Instance.CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:
                    localPlayerController.cursorTip.text = UIConst.UI_CHOOSE_LOCATION;
                    break;

                case EnumInputAction.SendingAllInternsToLocation:
                    localPlayerController.cursorTip.text = UIConst.UI_CHOOSE_LOCATION;
                    break;

                case EnumInputAction.None:
                default:
                    ClearCursorTipText();
                    break;
            }
        }

        public void InitUI(PlayerControllerB player)
        {
            localPlayerController = player;

            if (CanvasOverlay == null)
            {
                CanvasOverlay = gameObject.AddComponent<Canvas>();
                CanvasOverlay.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = CanvasOverlay.gameObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            if (iconUIDispenser == null)
            {
                iconUIDispenser = new IconUIDispenser(CanvasOverlay);
            }
        }

        public void InitCommandsUI(Transform HUDContainerParent)
        {
            Plugin.LogDebug($"Attempt to InitCommandsUI");
            if (CommandsSingleIntern != null)
            {
                Plugin.LogDebug($"InitCommandsUI aborting : CommandsUI != null");
                return;
            }

            CommandsSingleIntern = GameObject.Instantiate(Plugin.CommandsSingleInternUIPrefab, HUDContainerParent);
            CommandsSingleIntern.name = "CommandsSingleInternUI";
            CommandsSingleIntern.SetActive(false);
            foreach (CommandButtonController commandButtonController in CommandsSingleIntern.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                Plugin.LogDebug($"CommandsSingleIntern commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }

            CommandsMultipleInterns = GameObject.Instantiate(Plugin.CommandsMultipleInternsUIPrefab, HUDContainerParent);
            CommandsMultipleInterns.name = "CommandsMultipleInternsUI";
            CommandsMultipleInterns.SetActive(false);
            foreach (CommandButtonController commandButtonController in CommandsMultipleInterns.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                Plugin.LogDebug($"CommandsMultipleInterns commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }
        }

        private void CommandWheelButtonController_OnSelected(object sender, EventArgs e)
        {
            CommandButtonController commandButtonController = (CommandButtonController)sender;
            Plugin.LogDebug($"commandWheelButtonController? sender {commandButtonController.ID} {(EnumUIButtonSelected)commandButtonController.ID}, e {e}, interns ? {InternsOwned}");

            if (!InternsOwned)
            {
                HideCommands();
                InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);
                return;
            }

            switch ((EnumUIButtonSelected)commandButtonController.ID)
            {
                case EnumUIButtonSelected.SendInternTo:
                    HideCommandsSingle();
                    InputManager.Instance.SetCurrentInputAction(EnumInputAction.SendingInternToLocation);
                    break;

                case EnumUIButtonSelected.SendAllInternsTo:
                    HideCommandsMultiple();
                    InputManager.Instance.SetCurrentInputAction(EnumInputAction.SendingAllInternsToLocation);

                    break;
                default:
                    break;
            }
        }

        private IEnumerator ScanPosition()
        {
            while (InputManager.Instance.CurrentInputAction == EnumInputAction.SendingInternToLocation
                    || InputManager.Instance.CurrentInputAction == EnumInputAction.SendingAllInternsToLocation
                   && InternsOwned)
            {
                Ray interactRay = new Ray(localPlayerController.gameplayCamera.transform.position, localPlayerController.gameplayCamera.transform.forward);
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

        public Vector3? GetValidNavMeshPoint()
        {
            if (!isValidNavMeshPoint
                || !lastNavMeshHitPoint.HasValue)
            {
                return null;
            }

            GameObject[] allAINodes;
            if (lastNavMeshHitPoint.Value.y >= -80f)
            {
                allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            }
            else
            {
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            }

            return allAINodes.OrderBy(node => (node.transform.position - lastNavMeshHitPoint.Value).sqrMagnitude)
                             .FirstOrDefault()
                             .transform.position;
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


        //Plugin.LogDebug($"pos {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.position}, {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.GetSiblingIndex()}");

        //Component[] components = GroupCommandWheel.GetComponentsInChildren(typeof(Component));
        //Plugin.LogDebug($"==================");
        //Plugin.LogDebug($"GroupCommandWheel component n {components.Length}");
        //foreach (Component component in components)
        //{
        //    if (component == null) continue;

        //    component.transform.SetAsLastSibling();
        //    Plugin.LogDebug($"pos {component.transform.position}, index {component.transform.GetSiblingIndex()}, active {component.gameObject.activeSelf} {component.ToString()}");
        //}

        //Plugin.LogDebug($"GroupCommandWheel {GroupCommandWheel.activeSelf}");

        public void ShowCommandsSingle()
        {
            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;


            InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);

            CommandsSingleIntern.SetActive(true);
            CommandsMultipleInterns.SetActive(false);
        }

        public void HideCommandsSingle()
        {
            if (!IsCommandsSingleInternOpened)
            {
                return;
            }

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StopScanPositionCoroutine();

            CommandsSingleIntern.SetActive(false);
        }

        public void ShowCommandsMultiple()
        {
            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);

            CommandsSingleIntern.SetActive(false);
            CommandsMultipleInterns.SetActive(true);
        }

        public void HideCommandsMultiple()
        {
            if (!IsCommandsMultipleInternsOpened)
            {
                return;
            }

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StopScanPositionCoroutine();

            CommandsMultipleInterns.SetActive(false);
        }

        public void HideCommands()
        {
            HideCommandsSingle();
            HideCommandsMultiple();
        }

        public void ClearCursorTipText()
        {
            if (localPlayerController.cursorTip.text == UIConst.UI_CHOOSE_LOCATION)
            {
                localPlayerController.cursorTip.text = string.Empty;
            }
        }
    }
}
