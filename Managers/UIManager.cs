using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Interfaces;
using LethalInternship.Interns.AI;
using LethalInternship.UI;
using LethalInternship.UI.Icons;
using LethalInternship.UI.Icons.InputIcons;
using LethalInternship.UI.Icons.Pools;
using LethalInternship.UI.Icons.WorldIcons;
using LethalInternship.UI.Renderers;
using LethalInternship.UI.Renderers.InterestPointsRenderer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
        private WorldIconUIPool worldIconUIPool = null!;
        private InputIconUIPool inputIconUIPool = null!;

        // Input icon current icon
        private GameObject inputIconImagePrefab = null!;

        // Renderers
        InterestPointRendererRegistery interestPointRendererRegistery = null!;
        PointOfInterestRendererService pointOfInterestRendererService = null!;

        private PlayerControllerB localPlayerController = null!;
        private bool InternsOwned;
        private WorldIconUI? WorldIconInCenter = null;

        private void Awake() { Instance = this; }

        private void Start() { }

        private void Update()
        {
            if (!Plugin.UIAssetsLoaded)
            {
                return;
            }

            // Check for interns owned
            InternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
            InternsOwned = internsOwned.Length > 0;
            if (!InternsOwned)
            {
                InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);
            }

            // ----------------
            WorldIconInCenter = null;

            // Show other already active icons
            var pointsOfInterest = internsOwned
                         .Where(y => y.PointOfInterest != null)
                         .Select(x => x.PointOfInterest!)
                         .Distinct();
            foreach (IPointOfInterest pointOfInterest in pointsOfInterest)
            {
                //Plugin.LogDebug($"pointOfInterest {pointOfInterest.}");
                WorldIconUI worldIcon = worldIconUIPool.GetIcon(pointOfInterestRendererService.GetIconUIInfos(pointOfInterest));
                worldIcon.SetPositionUI(pointOfInterest.GetPoint());
                worldIcon.SetDefaultColor();
                worldIcon.SetIconActive(true);

                // Scan icon in center
                if (WorldIconInCenter == null)
                {
                    WorldIconInCenter = worldIcon.IsIconInCenter ? worldIcon : null;
                }
            }

            // Clear remaining icons
            worldIconUIPool.DisableOtherIcons();
        }

        private void LateUpdate()
        {
            if (!Plugin.UIAssetsLoaded)
            {
                return;
            }

            switch (InputManager.Instance.CurrentInputAction)
            {
                case EnumInputAction.SendingInternToLocation:
                case EnumInputAction.SendingAllInternsToLocation:
                    localPlayerController.cursorTip.text = UIConst.UI_CHOOSE_LOCATION;
                    break;

                case EnumInputAction.None:
                default:
                    ClearCursorTipText();
                    break;
            }
        }

        public void AttachUIToLocalPlayer(PlayerControllerB player)
        {
            localPlayerController = player;
        }

        public void InitUI(Transform HUDContainerParent)
        {
            if (!Plugin.UIAssetsLoaded)
            {
                Plugin.LogWarning("No UI initialization : UI assets failed to load (see Plugin loading assets).");
                return;
            }

            Plugin.LogDebug($"InitUI");

            if (CanvasOverlay == null)
            {
                CanvasOverlay = gameObject.AddComponent<Canvas>();
                CanvasOverlay.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = CanvasOverlay.gameObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            // Renderers
            interestPointRendererRegistery = new InterestPointRendererRegistery();
            interestPointRendererRegistery.Register(new DefaultInterestPointRenderer());

            pointOfInterestRendererService = new PointOfInterestRendererService(interestPointRendererRegistery);

            // Dispenser
            worldIconUIPool ??= new WorldIconUIPool(CanvasOverlay);
            inputIconUIPool ??= new InputIconUIPool(CanvasOverlay);

            // Instantiating prefabs
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

        public void ShowInputIcon(bool isValid)
        {
            InputIconUI inputIconUI = inputIconUIPool.GetIcon(new IconUIInfos(inputIconImagePrefab.name, new List<GameObject>() { inputIconImagePrefab }));
            inputIconUI.SetPositionUICenter();
            inputIconUI.SetColorIconValidOrNot(isValid);
            inputIconUI.SetIconActive(true);
        }

        public void SetDefaultInputIcon()
        {
            inputIconImagePrefab = Plugin.DefaultIconImagePrefab;
        }

        public Vector3? GetWorldIconInCenter()
        {
            return WorldIconInCenter?.IconWorldPosition;
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
            if (!Plugin.UIAssetsLoaded)
            {
                return;
            }

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

            CommandsSingleIntern.SetActive(false);
        }

        public void ShowCommandsMultiple()
        {
            if (!Plugin.UIAssetsLoaded)
            {
                return;
            }

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
