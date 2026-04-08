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
        private GameObject commandsAll = null!;
        public bool IsCommandsAllOpened { get { return commandsAll != null && commandsAll.activeSelf; } }

        // Commands one panel
        private GameObject commandsOne = null!;
        public bool IsCommandsOneOpened { get { return commandsOne != null && commandsOne.activeSelf; } }

        public bool IsAnyCommandsPanelOpened { get { return IsCommandsAllOpened || IsCommandsOneOpened; } }

        // TooltipBar
        private GameObject toolTipBarUI = null!;

        public TMP_FontAsset FontToUse => HUDManager.Instance.statsUIElements.playerNamesText[0].font;

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

        // Outlines
        private IInternAI? currentPointedIntern = null;
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

        private void ShowWorldIconUIs()
        {
            if (worldIconUIPool == null)
            {
                return;
            }

            PointOfInterestInCenter = null;

            // Check for interns owned
            IInternAI[] internsOwned = InternManager.Instance.GetAliveAndSpawnInternsAIOwnedByLocal();
            InternsOwned = internsOwned.Length > 0;
            if (!InternsOwned)
            {
                // Clear remaining icons
                worldIconUIPool.DisableOtherIcons();
                return;
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

            pointOfInterestRendererService = new PointOfInterestRendererService(interestPointRendererRegistery);

            // Dispenser
            worldIconUIPool ??= new WorldIconUIPool(CanvasOverlay);
            inputIconUIPool ??= new InputIconUIPool(CanvasOverlay);

            // Instantiating prefabs
            // ---------------------
            // CommandsAll
            if (commandsAll != null)
            {
                Object.Destroy(commandsAll);
            }
            commandsAll = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsAll, HUDContainerParent);
            commandsAll.SetActive(false);

            // CommandsOne
            if (commandsOne != null)
            {
                Object.Destroy(commandsOne);
            }
            commandsOne = GameObject.Instantiate(PluginRuntimeProvider.Context.CommandsOne, HUDContainerParent);
            commandsOne.SetActive(false);

            // Tooltip
            if (toolTipBarUI != null)
            {
                Object.Destroy(toolTipBarUI);
            }
            toolTipBarUI = GameObject.Instantiate(PluginRuntimeProvider.Context.TooltipBar, HUDContainerParent);
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
            inputIconUI.SetPositionUICenter();
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

            if (target.Value.PointOfInterest != null)
            {
                IIconUIInfos iconUIInfos = pointOfInterestRendererService.GetIconUIInfos(target.Value.PointOfInterest);
                return iconUIInfos.IconImagesTypes;
            }
            //else if(target.Value.Enemy != null)
            //{

            //}else if(target.Value.Item != null)
            //{

            //}

            return EnumIconImagesTypes.None;
        }

        public IPointOfInterest? GetPointOfInterestInCenter()
        {
            return PointOfInterestInCenter;
        }

        #region Show/Hide commands

        public void ToogleCommandsAll()
        {
            if (IsCommandsAllOpened)
            {
                HideCommandsAll();
            }
            else
            {
                ShowCommandsAll();
            }
        }

        public void ToogleCommandsOne()
        {
            if (IsCommandsOneOpened)
            {
                HideCommandsOne();
            }
            else
            {
                ShowCommandsOne();
            }
        }

        public void ShowCommandsAll()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
                return;
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            commandsAll.SetActive(true);
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

            commandsOne.SetActive(true);
        }

        public void ResetCommandsOne()
        {
            if (!PluginRuntimeProvider.Context.UIAssetsLoaded)
                return;
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
                return;

            if (!IsCommandsOneOpened)
                ShowCommandsOne();

            commandsOne.GetComponent<CommandsOneController>().Init();
        }

        public void HideCommandsAll(bool resetCameraFocus = true)
        {
            if (!IsCommandsAllOpened)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            TooltipBarUI.Instance.Hide();

            if (resetCameraFocus)
                CameraFocusUI.Instance.ReturnToInitial();

            commandsAll.SetActive(false);
        }

        public void HideCommandsOne()
        {
            if (!IsCommandsOneOpened)
                return;

            GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            TooltipBarUI.Instance.Hide();
            CameraFocusUI.Instance.ReturnToInitial();

            commandsOne.SetActive(false);
        }

        public void HideAll()
        {
            HideCommandsAll();
            HideCommandsOne();
        }

        #endregion

        public void ClearCursorTipText()
        {
            if (localPlayerController.cursorTip.text == UIConst.UI_CHOOSE_LOCATION)
            {
                localPlayerController.cursorTip.text = string.Empty;
            }
        }

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
                tooltipsToAdd.Add(("commands", string.Format(UIConst.TOOLTIP_COMMANDS,
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
            if (isSeparatorToAdd || !string.IsNullOrWhiteSpace(baseText))
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

        public void UpdateCursorTooltipsOfPointedIntern()
        {
            // TODO: rework with targeting manager
            IInternAI? intern = currentPointedIntern;
            if (intern == null)
            {
                return;
            }

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            StringBuilder sb = new StringBuilder();
            float distance = intern.NpcController.GetSqrDistanceWithLocalPlayer();
            if (distance < localPlayer.grabDistance * localPlayer.grabDistance)
            {
                // Line grab/drop item
                if (!intern.AreHandsFree())
                {
                    sb.Append(string.Format(UIConst.TOOLTIP_DROP_ITEM, InputManagerProvider.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern)))
                        .AppendLine();
                }
                else if (localPlayer.currentlyHeldObjectServer != null)
                {
                    sb.Append(string.Format(UIConst.TOOLTIP_TAKE_ITEM, InputManagerProvider.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.GiveItemToIntern)))
                        .AppendLine();
                }

                // Line Follow manage
                if (intern.OwnerClientId != localPlayer.actualClientId)
                {
                    sb.Append(string.Format(UIConst.TOOLTIP_FOLLOW_ME, InputManagerProvider.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern)))
                        .AppendLine();
                }

                // Grab intern
                sb.Append(string.Format(UIConst.TOOLTIP_GRAB_INTERNS, InputManagerProvider.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern)))
                    .AppendLine();
            }

            // Open commands for intern
            //sb.Append(string.Format(UIConst.TOOLTIP_COMMANDS, InputManagerProvider.Instance.GetKeyAction(PluginRuntimeProvider.Context.InputActionsInstance.OpenCommandsIntern)))
            //    .AppendLine();

            localPlayer.cursorTip.text = sb.ToString();
        }

        #endregion

        #region Outlines

        private void UpdateCurrentPointedIntern()
        {
            // Look for almost pointed on intern
            IInternAI? bestPointedIntern = FindPointedIntern(tightAngle: true);
            if (bestPointedIntern != null)
            {
                currentPointedIntern = bestPointedIntern;
                return;
            }

            // Keep pointed intern outline from flickering between other interns
            if (currentPointedIntern == null
                || !IsPointedInternStillValid(currentPointedIntern))
            {
                currentPointedIntern = FindPointedIntern(tightAngle: false);
            }
        }

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

            // Update intern outlines
            InternOutlineController.UpdateOutlines(InternManager.Instance.GetAliveAndSpawnInternsAIOwnedByLocal(),
                                                   target?.Intern?.Npc.playerClientId,
                                                   allowMultipleInternOutline,
                                                   forceNoOutlines: IsAnyCommandsPanelOpened);
        }

        private bool IsPointedInternStillValid(IInternAI internAI)
        {
            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;

            if (StartOfRound.Instance.localPlayerController.isInsideFactory != internAI.Npc.isInsideFactory)
            {
                return false;
            }
            // No action if in spawning animation
            if (internAI.IsSpawningAnimationRunning())
            {
                return false;
            }

            float distance = internAI.NpcController.GetSqrDistanceWithLocalPlayer();
            float angle = internAI.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internAI.Npc.transform.position + new Vector3(0f, 1f, 0f));
            float allowedAngle = GetAllowedAngle(distance, tightAngle: false) + 1.5f; // anti flickering margin

            return angle <= allowedAngle
                && HasPlayerLineOfSightOnIntern(internAI);
        }

        private float GetAllowedAngle(float distance, bool tightAngle)
        {
            float minDistance = Mathf.Pow(1f, 2);   // very close
            float maxDistance = Mathf.Pow(15f, 2);  // far

            float maxAngleClose = tightAngle ? 10f : 20f; // degrees when very close
            float maxAngleFar = tightAngle ? 1f : 4f;  // degrees when far

            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            return Mathf.Lerp(maxAngleClose, maxAngleFar, t);
        }

        private bool HasPlayerLineOfSightOnIntern(IInternAI internAI)
        {
            return !Physics.Linecast(StartOfRound.Instance.localPlayerController.gameplayCamera.transform.position,
                                     internAI.Npc.transform.position
                                        + new Vector3(0f, 2f * PluginRuntimeProvider.Context.Config.InternSizeScale * 0.80f, 0f),
                                     StartOfRound.Instance.collidersAndRoomMaskAndDefault,
                                     QueryTriggerInteraction.Ignore);
        }

        private IInternAI? FindPointedIntern(bool tightAngle)
        {
            IInternAI? bestPointedIntern = null;
            //float bestScore = float.MaxValue;

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            IInternAI[] internAIs = InternManager.Instance.GetAliveAndSpawnInternsAI();
            foreach (IInternAI internAI in internAIs)
            {
                if (StartOfRound.Instance.localPlayerController.isInsideFactory != internAI.Npc.isInsideFactory)
                {
                    continue;
                }
                // No action if in spawning animation
                if (internAI.IsSpawningAnimationRunning())
                {
                    continue;
                }

                float distance = internAI.NpcController.GetSqrDistanceWithLocalPlayer();
                float angle = internAI.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internAI.Npc.transform.position
                                                                                               + new Vector3(0f, 2f * PluginRuntimeProvider.Context.Config.InternSizeScale * 0.80f, 0f));
                float allowedAngle = GetAllowedAngle(distance, tightAngle);

                if (angle > allowedAngle)
                {
                    continue;
                }

                if (!HasPlayerLineOfSightOnIntern(internAI))
                {
                    continue;
                }

                // Score best pointed intern
                //float score = angle * angleWeight + distance * distanceWeight;
                //if (score < bestScore)
                //{
                //    bestScore = score;
                //    bestPointedIntern = internAI;
                //}
            }
            return bestPointedIntern;
        }

        #endregion
    }
}
