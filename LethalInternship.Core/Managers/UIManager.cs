using GameNetcodeStuff;
using LethalInternship.Core.UI;
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.Managers
{
    public class UIManager : MonoBehaviour, IUIManager
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
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            // Check for interns owned
            IInternAI[] internsOwned = InternManager.Instance.GetInternsAIOwnedByLocal();
            InternsOwned = internsOwned.Length > 0;
            if (!InternsOwned)
            {
                InputManager.Instance.SetCurrentInputAction(EnumInputAction.None);
            }

            // ----------------
            WorldIconInCenter = null;

            // Show other already active icons
            var pointsOfInterest = internsOwned
                         .Where(y => y.GetPointOfInterest() != null)
                         .Select(x => x.GetPointOfInterest()!)
                         .Distinct();
            foreach (IPointOfInterest pointOfInterest in pointsOfInterest)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"pointOfInterest {pointOfInterest.}");
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
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
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

            pointOfInterestRendererService = new PointOfInterestRendererService(interestPointRendererRegistery);

            // Dispenser
            worldIconUIPool ??= new WorldIconUIPool(CanvasOverlay);
            inputIconUIPool ??= new InputIconUIPool(CanvasOverlay);

            // Instantiating prefabs
            CommandsSingleIntern = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsSingleInternUIPrefab, HUDContainerParent);
            CommandsSingleIntern.name = "CommandsSingleInternUI";
            CommandsSingleIntern.SetActive(false);
            foreach (CommandButtonController commandButtonController in CommandsSingleIntern.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                PluginLoggerHook.LogDebug?.Invoke($"CommandsSingleIntern commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }

            CommandsMultipleInterns = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsMultipleInternsUIPrefab, HUDContainerParent);
            CommandsMultipleInterns.name = "CommandsMultipleInternsUI";
            CommandsMultipleInterns.SetActive(false);
            foreach (CommandButtonController commandButtonController in CommandsMultipleInterns.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                PluginLoggerHook.LogDebug?.Invoke($"CommandsMultipleInterns commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }
        }

        private void CommandWheelButtonController_OnSelected(object sender, EventArgs e)
        {
            CommandButtonController commandButtonController = (CommandButtonController)sender;
            PluginLoggerHook.LogDebug?.Invoke($"commandWheelButtonController? sender {commandButtonController.ID} {(EnumUIButtonSelected)commandButtonController.ID}, e {e}, interns ? {InternsOwned}");

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
            inputIconImagePrefab = PluginRuntimeProvider.Context.DefaultIconImagePrefab;
        }

        public Vector3? GetWorldIconInCenter()
        {
            return WorldIconInCenter?.IconWorldPosition;
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

        public void ShowCommandsSingle()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
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
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
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
