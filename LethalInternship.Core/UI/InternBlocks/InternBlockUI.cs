using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.InternBlocks
{
    public class InternBlockUI : MonoBehaviour,
        IRefreshableUI,
        IGroupUI,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        private enum EnumObjectiveIcon
        {
            None = 0,
            Follow,
            Vehicle,
            ToPosition,
            Scavenge,
            FetchItem,
            Fighting,
            DropItem,
        }

        private readonly StringBuilder _sb = new StringBuilder(128);

        public static System.Action OnSelected = null!;

        public TextMeshProUGUI NameText = null!;
        public TextMeshProUGUI ItemCountText = null!;

        public Image BackgroundImage = null!;

        public Image ObjectiveIcon = null!;
        public Image BehaviourIcon = null!;

        public Sprite[] ObjectiveSprites = null!;

        public Sprite SpriteAutoDefense = null!;
        public Sprite SpriteFlee = null!;
        public Sprite SpriteDead = null!;

        private IInternIdentity identity = null!;

        // Typing animation
        private string fullText = string.Empty;
        private string cursorChar = "$";
        private float cursorBlink = 0.2f;
        private Coroutine typingCoroutine = null!;
        private Coroutine cursorCoroutine = null!;
        private bool showCursor = true;
        private string currentText = string.Empty;

        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;

        private EnumObjectiveIcon currentObjective = EnumObjectiveIcon.Follow;
        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

        private bool isStateValid => !isNotInteractable && IdentityManager.Instance.IsIdentityValidToCommand(identity);

        public EnumUIGroups GroupUI => EnumUIGroups.InternsList;

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;
            NameText.font = fontToUse;
            ItemCountText.font = fontToUse;

            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();

            if (!string.IsNullOrWhiteSpace(fullText))
            {
                // Typing animation
                NameText.text = string.Empty;
                currentText = string.Empty;
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            if (isNotInteractable == interactable)
            {
                isNotInteractable = !interactable;
                Refresh();
            }

            UpdateHighlight(forceUpdate: true);
        }

        public void Setup(IInternIdentity i)
        {
            identity = i;
            fullText = i.Name;

            // Typing animation
            NameText.text = string.Empty;
            currentText = string.Empty;
            typingCoroutine = StartCoroutine(TypeText());
            cursorCoroutine = StartCoroutine(CursorBlink());

            Refresh();
        }

        public void Refresh()
        {
            ItemCountText.transform.parent.gameObject.SetActive(isStateValid);
            ObjectiveIcon.transform.parent.gameObject.SetActive(isStateValid);
            ObjectiveIcon.transform.parent.gameObject.SetActive(isStateValid);

            if (isStateValid)
            {
                UpdateItemCount();
                UpdateObjective();
                UpdateBehaviour();
            }
            else if (!identity.Alive)
            {
                // Dead
                ObjectiveIcon.transform.parent.gameObject.SetActive(true);
                BehaviourIcon.sprite = SpriteDead;
                StopHover();
            }
            else
            {
                StopHover();
            }
        }

        public void UpdateItemCount()
        {
            if (!isStateValid) return;

            ItemCountText.transform.parent.gameObject.SetActive(true);
            IInternAI? intern = identity.InternAI;
            if (intern != null)
            {
                ItemCountText.text = intern.GetNbHeldItems().ToString();
            }
        }

        public void UpdateObjective()
        {
            if (!isStateValid) return;

            ObjectiveIcon.transform.parent.gameObject.SetActive(true);

            if (ObjectiveSprites == null
                || ObjectiveSprites.Length == 0
                || identity == null
                || identity.InternAI == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"InternBlockUI no objective sprites available !");
                return;
            }

            currentObjective = GetObjectiveFromCommand(identity.InternAI.CurrentCommand);
            ObjectiveIcon.sprite = ObjectiveSprites[(int)currentObjective];
        }

        private EnumObjectiveIcon GetObjectiveFromCommand(EnumCommandTypes commandType)
        {
            switch (commandType)
            {
                case EnumCommandTypes.None:
                    return EnumObjectiveIcon.None;

                case EnumCommandTypes.FollowPlayer:
                    return EnumObjectiveIcon.Follow;

                case EnumCommandTypes.GoToVehicle:
                    return EnumObjectiveIcon.Vehicle;

                case EnumCommandTypes.GoToPosition:
                    return EnumObjectiveIcon.ToPosition;

                case EnumCommandTypes.WaitForCommand:
                    return currentObjective;

                case EnumCommandTypes.ScavengingToShip:
                    return EnumObjectiveIcon.Scavenge;

                case EnumCommandTypes.ScavengingToCruiser:
                    return EnumObjectiveIcon.Scavenge;

                case EnumCommandTypes.ScavengingToGatheringPoint:
                    return EnumObjectiveIcon.Scavenge;

                case EnumCommandTypes.GoFetchItem:
                    return EnumObjectiveIcon.FetchItem;

                case EnumCommandTypes.Kill:
                    return EnumObjectiveIcon.Fighting;

                case EnumCommandTypes.DropAllItemsToShip:
                    return EnumObjectiveIcon.DropItem;

                case EnumCommandTypes.DropAllItemsOnGatheringPoint:
                    return EnumObjectiveIcon.DropItem;

                case EnumCommandTypes.DropAllItemsInCruiser:
                    return EnumObjectiveIcon.DropItem;

                case EnumCommandTypes.UnloadCruiser:
                    return EnumObjectiveIcon.DropItem;

                case EnumCommandTypes.UnloadGatheringPoint:
                    return EnumObjectiveIcon.DropItem;

                default:
                    return EnumObjectiveIcon.None;
            }
        }

        public void UpdateBehaviour()
        {
            if (!isStateValid) return;

            BehaviourIcon.sprite = identity.AutoDefense ? SpriteAutoDefense : SpriteFlee;
        }

        private void StartHover()
        {
            SetAlpha(BackgroundImage, 1f);
        }

        private void StopHover()
        {
            SetAlpha(BackgroundImage, 100f / 255f);
        }

        private void SetAlpha(Image image, float transparency)
        {
            if (image != null
                && image.color.a != transparency)
            {
                Color alpha = image.color;
                alpha.a = transparency;
                image.color = alpha;
            }
        }

        private IEnumerator TypeText()
        {
            foreach (char c in fullText)
            {
                currentText += c;
                UpdateText();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
            }
        }

        private IEnumerator CursorBlink()
        {
            while (currentText != fullText)
            {
                showCursor = !showCursor;
                UpdateText();
                yield return new WaitForSeconds(cursorBlink);
            }
            // No cursor after the end
            showCursor = false;
            UpdateText();
        }

        private void UpdateText()
        {
            NameText.text = currentText + (showCursor ? cursorChar : " ");
        }

        private string SetTooltipMessage()
        {
            _sb.Clear();
            if (identity == null || identity.InternAI == null)
                return _sb.ToString();

            _sb.Append(identity.InternAI.Npc.playerUsername);
            _sb.Append(" ");

            _sb.Append("[");
            _sb.Append(identity.InternAI.GetNbHeldItems().ToString());
            _sb.Append(" items] ");

            if (identity.InternAI.TempCommandFeedback == EnumTempCommandFeedback.ExecutingCommand)
            {
                if (identity.InternAI.PendingCommand == EnumCommandTypes.WaitForCommand)
                    _sb.Append(UIConst.TOOLTIP_EXECUTING_COMMAND[(int)identity.InternAI.CurrentCommand]);
                else
                    _sb.Append(UIConst.TOOLTIP_EXECUTING_COMMAND[(int)identity.InternAI.PendingCommand]);
            }
            else
                _sb.Append(UIConst.TOOLTIP_COMMAND_FEEDBACK[(int)identity.InternAI.TempCommandFeedback]);

            _sb.Append(" ");
            if (identity.AutoDefense)
                _sb.Append(UIConst.TOOLTIP_AUTODEFENSE_BEHAVIOUR);
            else
                _sb.Append(UIConst.TOOLTIP_FLEE_BEHAVIOUR);

            return _sb.ToString();
        }

        private void UpdateHighlight(bool forceUpdate = false)
        {
            bool highlighted = isPointerOver || isSelected;

            if (highlighted == isHovered
                && !forceUpdate)
                return;

            isHovered = highlighted;

            if (isHovered)
                StartHover();
            else
                StopHover();
        }

        #region Mouse events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isStateValid) return;

            if (identity.InternAI != null)
                CameraFocusUI.Instance.FocusOnIntern(identity.InternAI.Npc.transform);

            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = true;
            isSelected = false;
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);
            UpdateHighlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            isSelected = false;

            UIManager.Instance.ToolTipBarUI.Hide();
            UpdateHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isStateValid) return;

            IdentitySelectionService.Instance.SelectSingle(identity);
            OnSelected?.Invoke();
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
            if (!isStateValid) return;

            if (identity.InternAI != null)
                CameraFocusUI.Instance.FocusOnIntern(identity.InternAI.Npc.transform);

            UIManager.Instance.UpdateLastSelectedUI(this.gameObject);
            isPointerOver = false;
            isSelected = true;
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);
            UpdateHighlight();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isPointerOver = false;
            isSelected = false;

            UIManager.Instance.ToolTipBarUI.Hide();
            UpdateHighlight();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isStateValid) return;

            IdentitySelectionService.Instance.SelectSingle(identity);
            OnSelected?.Invoke();
        }

        #endregion
    }
}
