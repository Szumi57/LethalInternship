using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class GatheringPointController : MonoBehaviour,
        IVisibilityUI,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        public static System.Action<EnumInputAction> OnSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.GatheringPointGroupButtons;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public Image BgImage = null!;

        public Image SetImage = null!;
        public Image GoToImage = null!;

        public TextMeshProUGUI TMPDescription = null!;

        public RemoveGatheringPointController removeGatheringPointController = null!;

        public bool CanSetUpGatheringPoint;

        private float transparency = 1f;
        private float transparencyNotInteractable = 0.2f;
        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;

        // isGatheringPointSet
        private bool isGatheringPointSet => InternManager.Instance.GatheringPoint != null;

        // Typing animation
        private string fullText = string.Empty;
        private string cursorChar = "$";
        private float cursorBlink = 0.2f;
        private Coroutine typingCoroutine = null!;
        private Coroutine cursorCoroutine = null!;
        private bool showCursor = true;
        private string currentText = string.Empty;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

        void Awake()
        {
            SetAlpha(SetImage, 1f);
            SetAlpha(GoToImage, 0f);
            Go = this.gameObject;

            if (CanSetUpGatheringPoint)
                RemoveGatheringPointController.OnSelected += RemoveGatheringPoint_OnSelected;
        }

        void OnEnable()
        {
            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);

            UpdateStateRemoveButton();
            UpdateIconAndDesc();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            bool managingInterns = IdentitySelectionService.Instance.GetSelected()
                                        .Any(x => IdentityManager.Instance.IsIdentityValidToCommand(x));
            bool restrictedLocation = StartOfRound.Instance != null && (StartOfRound.Instance.inShipPhase
                                                                    || StartOfRound.Instance.shipIsLeaving
                                                                    || InternManager.Instance.IsCurrentMoonCompanyMoon());

            interactable = managingInterns && !restrictedLocation;
            isNotInteractable = !interactable;
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            UpdateIconAndDesc();
            UpdateHighlight(forceUpdate: true);
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

        private EnumInputAction GetCurrentInputAction()
        {
            if (CanSetUpGatheringPoint)
                return isGatheringPointSet ? EnumInputAction.GoToGatheringPoint : EnumInputAction.SetGatheringPoint;
            else
                return EnumInputAction.GoToGatheringPoint;
        }

        private void SetTMPDescriptionFont(TMP_FontAsset font)
        {
            if (TMPDescription != null)
            {
                TMPDescription.font = font;
            }
        }

        private void StartHover()
        {
            SetAlpha(BgImage, isNotInteractable ? transparencyNotInteractable : transparency);
            BgImage.pixelsPerUnitMultiplier = 7;

            fullText = UIConst.COMMANDS_BUTTON_STRING[(int)GetCurrentInputAction()];
            StopTypeTextCoroutine();
            if (TMPDescription != null)
            {
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        private void StopHover()
        {
            if (isGatheringPointSet)
            {
                SetAlpha(BgImage, isNotInteractable ? transparencyNotInteractable : transparency);
                BgImage.pixelsPerUnitMultiplier = 25;
            }
            else
                SetAlpha(BgImage, 0f);

            // Typing animation
            StopTypeTextCoroutine();
        }

        private void UpdateIconAndDesc()
        {
            if (!CanSetUpGatheringPoint)
            {
                SetAlpha(GoToImage, isNotInteractable ? transparencyNotInteractable : transparency);
                SetAlpha(SetImage, 0f);
                return;
            }

            if (isGatheringPointSet)
            {
                SetAlpha(GoToImage, isNotInteractable ? transparencyNotInteractable : transparency);
                SetAlpha(SetImage, 0f);
            }
            else
            {
                SetAlpha(GoToImage, 0f);
                SetAlpha(SetImage, isNotInteractable ? transparencyNotInteractable : transparency);
            }
        }

        private void UpdateStateRemoveButton()
        {
            StartCoroutine(CoroutineUpdateStateRemoveButton());
        }

        private IEnumerator CoroutineUpdateStateRemoveButton()
        {
            removeGatheringPointController.gameObject.SetActive(CanSetUpGatheringPoint && isGatheringPointSet);
            yield return null;
            removeGatheringPointController.gameObject.SetActive(CanSetUpGatheringPoint && isGatheringPointSet);
        }

        private void RemoveGatheringPoint_OnSelected()
        {
            if (!this.isActiveAndEnabled)
                return;

            // Gathering point is removed, does not wait for InternManager.Instance.GatheringPoint != null
            SetAlpha(GoToImage, 0f);
            SetAlpha(SetImage, 1f);

            if (InputManager.Instance.IsUsingController)
                EventSystem.current.SetSelectedGameObject(this.gameObject);

            if (removeGatheringPointController != null)
                removeGatheringPointController.gameObject.SetActive(false);
        }

        private void StopTypeTextCoroutine()
        {
            StopAllCoroutines();
            if (TMPDescription != null)
            {
                TMPDescription.text = string.Empty;
                currentText = string.Empty;
            }
        }

        IEnumerator TypeText()
        {
            foreach (char c in fullText)
            {
                currentText += c;
                UpdateText();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
            }
        }

        IEnumerator CursorBlink()
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

        void UpdateText()
        {
            TMPDescription.text = currentText + (showCursor ? cursorChar : " ");
        }

        private string SetTooltipMessage()
        {
            return UIConst.COMMANDS_BUTTON_STRING[(int)GetCurrentInputAction()];
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
            if (eventData.pointerPress == removeGatheringPointController.gameObject
                || eventData.pointerPress.transform.IsChildOf(removeGatheringPointController.gameObject.transform))
                return;

            if (isNotInteractable) return;

            OnSelected?.Invoke(GetCurrentInputAction());

            UpdateIconAndDesc();
            UpdateStateRemoveButton();
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
            if (!InputManager.Instance.IsUsingController)
                return;

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
            if (isNotInteractable) return;

            OnSelected?.Invoke(GetCurrentInputAction());

            UpdateIconAndDesc();
            UpdateStateRemoveButton();
        }

        #endregion
    }
}
