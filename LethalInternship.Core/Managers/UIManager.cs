using GameNetcodeStuff;
using LethalInternship.Core.UI.CommandsControllers;
using LethalInternship.Core.UI.Icons;
using LethalInternship.Core.UI.Icons.InputIcons;
using LethalInternship.Core.UI.Icons.Pools;
using LethalInternship.Core.UI.Icons.WorldIcons;
using LethalInternship.Core.UI.Others;
using LethalInternship.Core.UI.Outlines;
using LethalInternship.Core.UI.Renderers;
using LethalInternship.Core.UI.Renderers.InterestPointsRenderer;
using LethalInternship.Core.UI.TooltipBar;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using LethalInternship.SharedAbstractions.UI;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LethalInternship.Core.Managers.TargetingManager;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.Managers
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager _instance = null!;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(UIManager));
                    _instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Commands all panel
        private GameObject commandsAllGo = null!;
        public CommandsAllController CommandsAllController { get; private set; } = null!;
        public bool IsCommandsAllOpened { get { return commandsAllGo != null && commandsAllGo.activeSelf; } }

        // Commands one panel
        private GameObject commandsOneGo = null!;
        public CommandsOneController CommandsOneController { get; private set; } = null!;
        public bool IsCommandsOneOpened { get { return commandsOneGo != null && commandsOneGo.activeSelf; } }

        public bool IsAnyMenuOpened { get { return IsCommandsAllOpened || IsCommandsOneOpened || (GameNetworkManager.Instance?.localPlayerController?.quickMenuManager.isMenuOpen ?? false); } }
        private bool wasAnyMenuOpened;

        // TooltipBar
        private GameObject toolTipBarUIGo = null!;
        public TooltipBarUI ToolTipBarUI { get; private set; } = null!;

        public TMP_FontAsset FontToUse => HUDManager.Instance.statsUIElements.playerNamesText[0].font;

        // Canvas overlay
        public Canvas CanvasOverlay = null!;

        // Icon dispenser
        private WorldIconUIPool worldIconUIPool = null!;
        private InputIconUIPool inputIconUIPool = null!;

        // Input icon anim
        private bool firstShowNeedAnim;

        // Renderers
        private InterestPointRendererRegistery interestPointRendererRegistery = null!;
        private PointOfInterestRendererService pointOfInterestRendererService = null!;

        private IPointOfInterest? PointOfInterestInCenter = null;
        private List<IPointOfInterest> pointOfInterestsAlreadyDisplayed = new List<IPointOfInterest>();

        // Outlines
        private bool allowMultipleInternOutline = false;

        private float timerUpdateTooltips;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            UIManagerProvider.Register(this);
        }

        private void OnDestroy()
        {
            UIManagerProvider.Unregister(this);
        }

        private void Update()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            // Icons
            ShowWorldIconUIs();

            UpdateBillBoard();
            UpdateOutlines();

            // Tooltips
            timerUpdateTooltips += Time.deltaTime;
            if (timerUpdateTooltips > 0.25f)
            {
                timerUpdateTooltips = 0f;
                UpdateControlTip(HUDManager.Instance);
            }
        }

        private void LateUpdate()
        {
            UpdateCursorTooltips();
        }

        private void ShowWorldIconUIs()
        {
            if (worldIconUIPool == null)
                return;

            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
                return;

            PointOfInterestInCenter = null;

            bool isAnyMenuWasClosed = wasAnyMenuOpened && !IsAnyMenuOpened;
            wasAnyMenuOpened = IsAnyMenuOpened;

            // Check if nothing to show
            if (IsAnyMenuOpened)
            {
                // Clear remaining icons
                worldIconUIPool.DisableOtherIcons();
                return;
            }

            IInternAI[] internsOwned = InternManager.Instance.GetAliveAndSpawnInternsAIOwnedByLocal();
            List<WorldIconUI> worldIconsToReturn = new List<WorldIconUI>();
            WorldIconUI worldIcon;
            // Show other already active icons
            var pointsOfInterestToShow = internsOwned
                         .Where(y => y.GetPointOfInterest() != null)
                         .Select(x => x.GetPointOfInterest()!)
                         .Distinct().ToList();
            if (InternManager.Instance.GatheringPoint != null)
                pointsOfInterestToShow.Add(InternManager.Instance.GatheringPoint);

            foreach (IPointOfInterest pointOfInterest in pointsOfInterestToShow)
            {
                //Debug.Log($"uimanager pointOfInterest GetUIKey {pointOfInterestRendererService.GetIconUIInfos(pointOfInterest).GetUIKey()}");
                //foreach (var ip in pointOfInterest.GetListInterestPoints())
                //    Debug.Log($"uimanager pointOfInterest ip {ip.GetType()}");

                worldIcon = worldIconUIPool.GetIcon(pointOfInterestRendererService.GetIconUIInfos(pointOfInterest));
                worldIcon.SetPositionUI(pointOfInterestRendererService.GetUIIcon(pointOfInterest));
                worldIcon.SetIconActive(true);
                worldIcon.ForceVisible(value: InputManager.Instance.CurrentTargetedAbility != null || isAnyMenuWasClosed);
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

            pointOfInterestsAlreadyDisplayed = pointsOfInterestToShow.ToList();

            // Clear remaining icons
            worldIconUIPool.DisableOtherIcons();
            foreach (var icon in worldIconsToReturn)
            {
                worldIconUIPool.ReturnIcon(icon);
            }
        }

        public void InitUI(Transform HUDContainerParent)
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                PluginLoggerHook.LogWarning?.Invoke("No UI initialization : UI assets failed to load (see Plugin loading assets).");
                return;
            }

            PluginLoggerHook.LogInfo?.Invoke($"UIManager : Initialization...");

            if (CanvasOverlay == null)
            {
                CanvasOverlay = gameObject.AddComponent<Canvas>();
                CanvasOverlay.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = CanvasOverlay.gameObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            // Renderers
            interestPointRendererRegistery = new InterestPointRendererRegistery();
            interestPointRendererRegistery.Register(new PositionInterestPointRenderer());
            interestPointRendererRegistery.Register(new VehicleInterestPointRenderer());
            interestPointRendererRegistery.Register(new ShipInterestPointRenderer());
            interestPointRendererRegistery.Register(new GatheringPointRenderer());

            pointOfInterestRendererService = new PointOfInterestRendererService(interestPointRendererRegistery);

            // Dispenser
            worldIconUIPool ??= new WorldIconUIPool(CanvasOverlay);
            inputIconUIPool ??= new InputIconUIPool(CanvasOverlay);

            // Instantiating prefabs
            // ---------------------
            // CommandsAll
            if (commandsAllGo != null)
            {
                Object.Destroy(commandsAllGo);
            }
            commandsAllGo = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsAll, HUDContainerParent);
            CommandsAllController = commandsAllGo.GetComponent<CommandsAllController>();
            commandsAllGo.SetActive(false);

            // CommandsOne
            if (commandsOneGo != null)
            {
                Object.Destroy(commandsOneGo);
            }
            commandsOneGo = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsOne, HUDContainerParent);
            CommandsOneController = commandsOneGo.GetComponent<CommandsOneController>();
            commandsOneGo.SetActive(false);

            // Tooltip
            if (toolTipBarUIGo != null)
            {
                Object.Destroy(toolTipBarUIGo);
            }
            toolTipBarUIGo = GameObject.Instantiate(PluginRuntimeProvider.Context.TooltipBar, HUDContainerParent);
            ToolTipBarUI = toolTipBarUIGo.GetComponent<TooltipBarUI>();
        }

        public void ShowInputIcon()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            if (inputIconUIPool == null)
            {
                return;
            }

            InputIconUI inputIconUI = inputIconUIPool.GetIcon(new IconUIInfos(GetInputIcon()));
            inputIconUI.SetIconActive(true);

            if (firstShowNeedAnim)
            {
                inputIconUI.PlayStartAnim();
                firstShowNeedAnim = false;
            }

            inputIconUIPool.DisableOtherIcons();
            inputIconUIPool.ReturnIcon(inputIconUI);
        }

        public void HideInputIcon()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
            {
                return;
            }

            firstShowNeedAnim = true;

            if (inputIconUIPool == null)
            {
                return;
            }

            inputIconUIPool.DisableOtherIcons();
        }

        private EnumIconImagesTypes GetInputIcon()
        {
            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null)
            {
                return EnumIconImagesTypes.None;
            }

            // Icon are set here
            if (target.Value.Item != null)
            {
                return EnumIconImagesTypes.FetchItem;
            }
            else if (target.Value.Enemy != null)
            {
                if (InternManager.Instance.IsEnemyKillable(target.Value.Enemy))
                    return EnumIconImagesTypes.Attack;
                else
                    return EnumIconImagesTypes.CantAttack;
            }
            else if (TargetingManager.Instance.ActiveSearch.HasFlag(TargetType.GatheringPoint))
            {
                return EnumIconImagesTypes.GatheringPoint;
            }
            else if (target.Value.PointedPointOfInterest != null)
            {
                IIconUIInfos iconUIInfos = pointOfInterestRendererService.GetIconUIInfos(target.Value.PointedPointOfInterest);
                return iconUIInfos.IconImagesTypes;
            }
            else
            {
                RaycastHit targetHit = target.Value.RaycastHit;
                if (TargetingManager.Instance.IsColliderFromVehicle(targetHit.collider))
                {
                    return EnumIconImagesTypes.Vehicle;
                }
                else if (TargetingManager.Instance.IsColliderFromShip(targetHit.collider))
                {
                    Transform? shipTransform = TargetingManager.Instance.GetParentShip(targetHit.collider.gameObject.transform);
                    if (shipTransform != null)
                        return EnumIconImagesTypes.Ship;
                }
                else
                    return EnumIconImagesTypes.Position;
            }

            return EnumIconImagesTypes.None;
        }

        public IPointOfInterest? GetPointOfInterestInCenter()
        {
            return PointOfInterestInCenter;
        }

        #region Show/Hide commands

        public void ShowCommandsAll()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
                return;
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            commandsAllGo.SetActive(true);
        }

        public void ShowCommandsOne()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
                return;
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            commandsOneGo.SetActive(true);
        }

        public void RefreshCommandsOne()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
                return;

            if (!IsCommandsOneOpened)
                ShowCommandsOne();

            commandsOneGo.GetComponent<CommandsOneController>().Refresh();
        }

        public void HideCommandsAll(bool resetCameraFocus = true)
        {
            if (!IsCommandsAllOpened)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ToolTipBarUI.Hide();

            if (resetCameraFocus)
                CameraFocusUI.Instance.ReturnToInitial();

            commandsAllGo.SetActive(false);
        }

        public void HideCommandsOne()
        {
            if (!IsCommandsOneOpened)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ToolTipBarUI.Hide();
            CameraFocusUI.Instance.ReturnToInitial();

            commandsOneGo.SetActive(false);
        }

        public void HideAll()
        {
            HideCommandsAll();
            HideCommandsOne();
        }

        #endregion

        #region CursorTooltip

        public void ClearCursorTipText()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            if (localPlayer.cursorTip.text == UIConst.UI_CHOOSE_LOCATION)
            {
                localPlayer.cursorTip.text = string.Empty;
            }
        }

        private void UpdateCursorTooltips()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null)
                return;

            List<(string id, string text)> tooltipsToAdd = new List<(string id, string text)>();

            // Targeting tooltip
            if (InputManager.Instance.CurrentTargetedAbility != null)
            {
                if (target.Value.Item != null)
                {
                    tooltipsToAdd.Add(("targetingItem", UIConst.TOOLTIP_TARGETING_ITEM));
                }
                else if (target.Value.Enemy != null)
                {
                    if (InternManager.Instance.IsEnemyKillable(target.Value.Enemy))
                        tooltipsToAdd.Add(("targetingKillableEnemy", UIConst.TOOLTIP_TARGETING_ENEMY));
                    else
                        tooltipsToAdd.Add(("targetingUnkillableEnemy", UIConst.TOOLTIP_TARGETING_UNKILLABLE_ENEMY));
                }
                else if (target.Value.PointedPointOfInterest != null)
                {
                    tooltipsToAdd.Add(("targetingPosition", UIConst.TOOLTIP_TARGETING_POSITION));
                }
            }
            else if (target.Value.Intern != null) // Not targeting command
            {
                IInternAI intern = target.Value.Intern;

                // Temp command feedback
                if (intern.TempCommandFeedback != EnumTempCommandFeedback.None)
                {
                    tooltipsToAdd.Add(("commandFeedback", intern.TempCommandFeedback.ToString()));
                }

                if (intern.NpcController.GetSqrDistanceWithLocalPlayer() < localPlayer.grabDistance * localPlayer.grabDistance)
                {
                    // Grab distance
                    // Line give item
                    if (localPlayer.currentlyHeldObjectServer != null)
                    {
                        tooltipsToAdd.Add(("giveItem", string.Format(UIConst.TOOLTIP_GIVE_ITEM,
                                                                     InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern))));
                    }

                    // Owning ?
                    if (intern.OwnerClientId != localPlayer.actualClientId)
                    {
                        // Line manage
                        tooltipsToAdd.Add(("manage", string.Format(UIConst.TOOLTIP_MANAGE,
                                                                   InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern))));
                    }

                    // Grab intern
                    tooltipsToAdd.Add(("manage", string.Format(UIConst.TOOLTIP_GRAB_INTERNS,
                                                               InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern))));
                }

                // Owning ?
                if (intern.OwnerClientId == localPlayer.actualClientId)
                {
                    // Line manage
                    tooltipsToAdd.Add(("commandsOne", string.Format(UIConst.TOOLTIP_COMMANDS_ONE,
                                                                    InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsOneIntern))));
                }
            }

            // Send tooltips
            SetTooltips(localPlayer.cursorTip,
                        isSeparatorToAdd: false,
                        tooltipsToAdd);
        }

        #endregion

        #region Tips top right display

        const string SEPARATOR = "--------------";
        const string TT_START = "<tt id=";

        private void UpdateControlTip(HUDManager hudManager)
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

            List<(string id, string text)> tooltipsToAdd = new List<(string id, string text)>();
            // Release grabbed interns
            if (InternManager.Instance.IsLocalPlayerHoldingInterns())
            {
                tooltipsToAdd.Add(("release", string.Format(UIConst.TOOLTIP_RELEASE_INTERNS,
                                                            InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns))));
            }

            // Intern commands 
            if (InternManager.Instance.GetAliveAndSpawnInternsAIOwnedByLocal().Length > 0)
            {
                tooltipsToAdd.Add(("commandsAll", string.Format(UIConst.TOOLTIP_COMMANDS_ALL,
                                                             InputManager.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.OpenAllCommandsIntern))));
            }

            SetTooltips(hudManager.controlTipLines[index],
                        isSeparatorToAdd: index > 0,
                        tooltipsToAdd);
        }

        private string MakeTooltip(string id, string text)
        {
            return $"\n<size=0>{TT_START}{id}></size>{text}";
        }

        private void SetTooltips(TextMeshProUGUI tmp,
                                 bool isSeparatorToAdd,
                                 List<(string id, string text)> tooltips)
        {
            string baseText = StripAllTooltips(tmp.text);

            if (tooltips.Count == 0)
            {
                tmp.text = baseText;
                return;
            }

            var sb = new StringBuilder(baseText);
            if (isSeparatorToAdd && !string.IsNullOrWhiteSpace(baseText))
            {
                sb.Append('\n').Append(SEPARATOR);
            }

            foreach (var tt in tooltips)
            {
                sb.Append(MakeTooltip(tt.id, tt.text));
            }

            tmp.text = sb.ToString();
        }

        private string StripAllTooltips(string src)
        {
            // remove all rows with <tt id=...>
            while (true)
            {
                int s = src.IndexOf(TT_START);
                if (s < 0)
                    break;

                // remonter au début de la ligne
                int lineStart = s;
                while (lineStart > 0 && src[lineStart - 1] != '\n')
                    lineStart--;

                int lineEnd = src.IndexOf('\n', s);
                if (lineEnd < 0)
                    lineEnd = src.Length;

                src = src.Remove(lineStart, lineEnd - lineStart);
            }

            // remove separator if present
            src = RemoveSeparator(src);
            return src.TrimEnd('\n');
        }

        private string RemoveSeparator(string src)
        {
            int s = src.IndexOf(SEPARATOR);
            if (s < 0)
                return src;

            // back to start of row
            int lineStart = s;
            while (lineStart > 0 && src[lineStart - 1] != '\n')
                lineStart--;

            // go to end of row
            int lineEnd = src.IndexOf('\n', s);
            if (lineEnd < 0)
                lineEnd = src.Length;
            else
                lineEnd += 1; // include '\n'

            return src.Remove(lineStart, lineEnd - lineStart);
        }

        #endregion

        #region Outlines

        private void UpdateBillBoard()
        {
            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null
                || target.Value.Intern == null)
            {
                return;
            }

            // Name billboard
            target.Value.Intern.NpcController.ShowFullNameBillboard();
        }

        private void UpdateOutlines()
        {
            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null)
                return;

            // Update intern outlines
            var internsToOuline = IdentityManager.Instance.GetIdentitiesSpawned().Select(x => x.InternAI!);
            InternOutlineController.UpdateInternsOutlines(internsToOuline,
                                                          target?.Intern?.Npc.playerClientId,
                                                          allowMultipleInternOutline,
                                                          forceNoOutlines: IsAnyMenuOpened);


            InternOutlineController.UpdateEnemiesOutlines(InternManager.Instance.GetEnemiesList(),
                                                          target?.Enemy,
                                                          allowMultipleInternOutline,
                                                          forceNoOutlines: IsAnyMenuOpened);

            InternOutlineController.UpdateItemsOutlines(InternManager.Instance.GetGrabbableObjectsList(),
                                                        target?.Item,
                                                        allowMultipleInternOutline,
                                                        forceNoOutlines: IsAnyMenuOpened);
        }

        #endregion
    }
}
