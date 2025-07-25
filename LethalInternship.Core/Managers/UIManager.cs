using GameNetcodeStuff;
using LethalInternship.Core.UI.CommandButton;
using LethalInternship.Core.UI.CommandsUI;
using LethalInternship.Core.UI.Icons;
using LethalInternship.Core.UI.Icons.InputIcons;
using LethalInternship.Core.UI.Icons.Pools;
using LethalInternship.Core.UI.Icons.WorldIcons;
using LethalInternship.Core.UI.Renderers;
using LethalInternship.Core.UI.Renderers.InterestPointsRenderer;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.Managers
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        public static UIManager Instance { get; private set; } = null!;

        // Commands wheel
        public GameObject CommandsWheel = null!;
        public bool IsCommandsWheelOpened { get { return CommandsWheel != null && CommandsWheel.activeSelf; } }
        private CommandsUIController CommandsUIController = null!;

        // Canvas overlay
        public Canvas CanvasOverlay = null!;

        // Icon dispenser
        private WorldIconUIPool worldIconUIPool = null!;
        private InputIconUIPool inputIconUIPool = null!;

        // Input icon current icon
        private GameObject inputIconImagePrefab = null!;

        // Renderers
        private InterestPointRendererRegistery interestPointRendererRegistery = null!;
        private PointOfInterestRendererService pointOfInterestRendererService = null!;

        private PlayerControllerB localPlayerController = null!;
        private bool InternsOwned;
        private IPointOfInterest? PointOfInterestInCenter = null;
        private List<IPointOfInterest> pointOfInterestsAlreadyDisplayed = new List<IPointOfInterest>();
        private Coroutine CoroutineUpdateRightPanel = null!;
        private IInternAI? internAIToManage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
        }

        private void Start() { }

        private void Update()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            // ----------------
            ShowWorldIconUIs();
        }

        private void LateUpdate()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            switch (InputManager.Instance.CurrentInputAction)
            {
                case EnumInputAction.GoToPosition:
                    localPlayerController.cursorTip.text = UIConst.UI_CHOOSE_LOCATION;
                    break;

                case EnumInputAction.None:
                default:
                    ClearCursorTipText();
                    break;
            }
        }

        private void ShowWorldIconUIs()
        {
            PointOfInterestInCenter = null;

            // Check for interns owned
            IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
            InternsOwned = internsOwned.Length > 0;
            if (!InternsOwned)
            {
                InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);
            }

            List<WorldIconUI> worldIconsToReturn = new List<WorldIconUI>();
            WorldIconUI worldIcon;
            // Show other already active icons
            var pointsOfInterest = internsOwned
                         .Where(y => y.GetPointOfInterest() != null)
                         .Select(x => x.GetPointOfInterest()!)
                         .Distinct();
            foreach (IPointOfInterest pointOfInterest in pointsOfInterest)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"pointOfInterest {pointOfInterest.}");
                worldIcon = worldIconUIPool.GetIcon(pointOfInterestRendererService.GetIconUIInfos(pointOfInterest));
                worldIcon.SetPositionUI(pointOfInterestRendererService.GetUIIcon(pointOfInterest));
                worldIcon.SetDefaultColor();
                worldIcon.SetIconActive(true);
                worldIconsToReturn.Add(worldIcon);

                // Scan icon in center
                if (PointOfInterestInCenter == null)
                {
                    PointOfInterestInCenter = worldIcon.IsIconInCenter ? pointOfInterest : null;
                }

                // Should use ping animation ?
                if (!pointOfInterestsAlreadyDisplayed.Contains(pointOfInterest))
                {
                    worldIcon.TriggerPingAnimation();
                }
            }

            pointOfInterestsAlreadyDisplayed = pointsOfInterest.ToList();

            // Clear remaining icons
            worldIconUIPool.DisableOtherIcons();
            foreach (var icon in worldIconsToReturn)
            {
                worldIconUIPool.ReturnIcon(icon);
            }
        }

        public void AttachUIToLocalPlayer(PlayerControllerB player)
        {
            localPlayerController = player;
        }

        public void InitUI(Transform HUDContainerParent)
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                PluginLoggerHook.LogWarning?.Invoke("No UI initialization : UI assets failed to load (see Plugin loading assets).");
                return;
            }

            PluginLoggerHook.LogDebug?.Invoke($"InitUI");

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
            interestPointRendererRegistery.Register(new VehicleInterestPointRenderer());
            interestPointRendererRegistery.Register(new ShipInterestPointRenderer());

            pointOfInterestRendererService = new PointOfInterestRendererService(interestPointRendererRegistery);

            // Dispenser
            worldIconUIPool ??= new WorldIconUIPool(CanvasOverlay);
            inputIconUIPool ??= new InputIconUIPool(CanvasOverlay);

            // Instantiating prefabs
            CommandsWheel = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsWheelUIPrefab, HUDContainerParent);
            foreach (CommandButtonController commandButtonController in CommandsWheel.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                PluginLoggerHook.LogDebug?.Invoke($"CommandsWheel commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }
            CommandsUIController = CommandsWheel.GetComponent<CommandsUIController>();
            CommandsUIController.SetFont(HUDManager.Instance.statsUIElements.playerNamesText[0].font);
            CommandsWheel.SetActive(false);
        }

        private void CommandWheelButtonController_OnSelected(object sender, EventArgs e)
        {
            CommandButtonController commandButtonController = (CommandButtonController)sender;
            PluginLoggerHook.LogDebug?.Invoke($"commandWheelButtonController? sender {commandButtonController.ID} {(EnumInputAction)commandButtonController.ID}, e {e}, interns ? {InternsOwned}");

            if (!InternsOwned)
            {
                HideCommandsWheel();
                InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);
                return;
            }

            EnumInputAction enumInputAction = (EnumInputAction)commandButtonController.ID;
            if (enumInputAction != EnumInputAction.None)
            {
                HideCommandsWheel();
            }
            switch (enumInputAction)
            {
                case EnumInputAction.GoToPosition:
                    InputManager.Instance.SetCurrentInputAction(enumInputAction, internAIToManage);
                    SetPedestrianInputIcon();
                    break;

                case EnumInputAction.FollowMe:
                case EnumInputAction.GoToShip:
                case EnumInputAction.GoToVehicle:
                    InputManager.Instance.SetCurrentInputAction(enumInputAction, internAIToManage);
                    break;

                default:
                    break;
            }
        }

        public void ShowInputIcon(bool isValid)
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            InputIconUI inputIconUI = inputIconUIPool.GetIcon(new IconUIInfos(inputIconImagePrefab.name, new List<GameObject>() { inputIconImagePrefab }));
            inputIconUI.SetPositionUICenter();
            inputIconUI.SetColorIconValidOrNot(isValid);
            inputIconUI.SetIconActive(true);

            inputIconUIPool.DisableOtherIcons();
            inputIconUIPool.ReturnIcon(inputIconUI);
        }

        public void HideInputIcon()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            inputIconUIPool.DisableOtherIcons();
        }

        public void SetDefaultInputIcon()
        {
            inputIconImagePrefab = PluginRuntimeProvider.Context.DefaultIconImagePrefab;
        }
        public void SetPedestrianInputIcon()
        {
            inputIconImagePrefab = PluginRuntimeProvider.Context.PedestrianIconImagePrefab;
        }
        public void SetVehicleInputIcon()
        {
            inputIconImagePrefab = PluginRuntimeProvider.Context.VehicleIconImagePrefab;
        }
        public void SetShipInputIcon()
        {
            inputIconImagePrefab = PluginRuntimeProvider.Context.ShipIconImagePrefab;
        }

        public IPointOfInterest? GetPointOfInterestInCenter()
        {
            return PointOfInterestInCenter;
        }

        //PluginLoggerHook.LogDebug?.Invoke($"pos {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.position}, {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.GetSiblingIndex()}");

        //Component[] components = GroupCommandWheel.GetComponentsInChildren(typeof(Component));
        //PluginLoggerHook.LogDebug?.Invoke($"==================");
        //PluginLoggerHook.LogDebug?.Invoke($"GroupCommandWheel component n {components.Length}");
        //foreach (Component component in components)
        //{
        //    if (component == null) continue;

        //    component.transform.SetAsLastSibling();
        //    PluginLoggerHook.LogDebug?.Invoke($"pos {component.transform.position}, index {component.transform.GetSiblingIndex()}, active {component.gameObject.activeSelf} {component.ToString()}");
        //}

        //PluginLoggerHook.LogDebug?.Invoke($"GroupCommandWheel {GroupCommandWheel.activeSelf}");

        public bool ShowCommandsWheel(IInternAI? internAIToManage = null)
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return false;
            }
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
            {
                return false;
            }
            if (InternManager.Instance.GetInternsAIOwnedByLocal().Length == 0)
            {
                return false;
            }

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            InputManager.Instance.SetCurrentInputAction(EnumInputAction.None, internAIToManage);
            this.internAIToManage = internAIToManage;

            if (CoroutineUpdateRightPanel != null)
            {
                StopCoroutine(CoroutineUpdateRightPanel);
            }
            CoroutineUpdateRightPanel = StartCoroutine(UpdateCommandsWheelUI(internAIToManage));

            CommandsWheel.SetActive(true);
            return true;
        }

        public void HideCommandsWheel()
        {
            if (!IsCommandsWheelOpened)
            {
                return;
            }

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            CommandsWheel.SetActive(false);
        }

        public void ClearCursorTipText()
        {
            if (localPlayerController.cursorTip.text == UIConst.UI_CHOOSE_LOCATION)
            {
                localPlayerController.cursorTip.text = string.Empty;
            }
        }

        private IEnumerator UpdateCommandsWheelUI(IInternAI? internAIToManage)
        {
            yield return null;

            while (IsCommandsWheelOpened)
            {
                // Buttons
                CommandButtonController? commandWheelController = CommandsUIController.CommandWheelController.GetGoToVehicleButton();
                if (commandWheelController != null)
                {
                    commandWheelController.IsNotAvailable = InternManager.Instance.VehicleController == null;
                }

                // Right panel
                if (internAIToManage == null)
                {
                    IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
                    CommandsUIController.SetTitleListInterns("Managing interns in proximity :");

                    StringBuilder sb = new StringBuilder();
                    foreach (IInternAI intern in internsOwned)
                    {
                        sb.Append("> ");
                        sb.Append(intern.Npc.playerUsername);
                        sb.Append("\n");
                    }
                    CommandsUIController.SetTextListInterns(sb.ToString());
                }
                else
                {
                    CommandsUIController.SetTitleListInterns("Managing intern :");
                    CommandsUIController.SetTextListInterns("> " + internAIToManage.Npc.playerUsername);
                }

                yield return null;
            }
        }

        #region Tips display

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
                WriteControlTipLine(hudManager.controlTipLines[index], Const.TOOLTIP_RELEASE_INTERNS, InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns));
            }
            if (InternManager.Instance.IsLocalPlayerNextToChillInterns())
            {
                WriteControlTipLine(hudManager.controlTipLines[index], Const.TOOLTIP_MAKE_INTERN_LOOK, InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition));
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
    }
}
