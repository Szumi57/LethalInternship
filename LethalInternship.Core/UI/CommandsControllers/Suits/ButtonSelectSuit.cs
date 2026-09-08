using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSelectSuit : MonoBehaviour,
        IVisibilityUI,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        public static System.Action<int> OnSuitSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.SuitMenu;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public SuitListPanel SuitListPanel = null!;
        public Transform SuitListContentTransform = null!;

        public GameObject SuitButtonPrefab = null!;

        public Image FrameImage = null!;
        public Image IconImage = null!;

        private List<int> spawnedSuits = new List<int>();
        private List<SuitListButton> suitListButtons = new List<SuitListButton>();

        private float transparencyFull = 1f;

        private bool isHovered;
        private bool isPointerOver;
        private bool isSelected;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

        void Awake()
        {
            Go = this.gameObject;
        }

        void OnEnable()
        {
            isPointerOver = false;
            isSelected = false;
            isHovered = false;
            StopHover();
            CloseSuitPanel();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
                SetAlpha(IconImage, 0.2f);
            else
                SetAlpha(IconImage, transparencyFull);

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

        private void Toggle()
        {
            bool enable = !SuitListPanel.gameObject.activeSelf;
            if (enable)
            {
                InternManager.Instance.GetListOfAvailableSuitIDs(spawnedSuits);
                if (spawnedSuits.Count > 0)
                {
                    SuitListPanel.gameObject.SetActive(true);
                    Populate(spawnedSuits);
                }
            }
            else
            {
                CloseSuitPanel();
            }
        }

        public bool CloseSuitPanel()
        {
            if (!SuitListPanel.gameObject.activeSelf)
                return false;

            SuitListPanel.gameObject.SetActive(false);
            if (InputManager.Instance.IsUsingController)
                EventSystem.current.SetSelectedGameObject(this.gameObject);

            return true;
        }

        private void Populate(List<int> suits)
        {
            suitListButtons.Clear();
            foreach (Transform c in SuitListContentTransform)
                Destroy(c.gameObject);

            for (int i = 0; i < suits.Count; i++)
            {
                int suitID = suits[i];
                var item = Instantiate(SuitButtonPrefab, SuitListContentTransform);
                TMP_Text tMP_Text = item.GetComponentInChildren<TMP_Text>();
                tMP_Text.text = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
                tMP_Text.font = UIManager.Instance.FontToUse;

                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectSuit(suitID);
                });

                SuitListButton suitListButton = item.GetComponent<SuitListButton>();
                suitListButton.OnHover = () =>
                {
                    SuitListPanel.SetFocus(true);
                };
                suitListButton.OnUnhover = () =>
                {
                    StartCoroutine(CheckFocus());
                };
                suitListButtons.Add(suitListButton);

                if (i == 0
                    && InputManager.Instance.IsUsingController)
                    EventSystem.current.SetSelectedGameObject(suitListButton.gameObject);
            }
        }

        private void SelectSuit(int suitID)
        {
            OnSuitSelected?.Invoke(suitID);
        }

        private IEnumerator CheckFocus()
        {
            yield return null;
            foreach (SuitListButton suitListButton in suitListButtons)
            {
                if (suitListButton.IsHovered)
                {
                    yield break;
                }
            }
            SuitListPanel.SetFocus(false);
        }

        private void StartHover()
        {
            FrameImage.pixelsPerUnitMultiplier = 10f;
        }

        private void StopHover()
        {
            FrameImage.pixelsPerUnitMultiplier = 20f;
        }

        private string SetTooltipMessage()
        {
            return UIConst.COMMANDS_BUTTON_STRING[(int)EnumInputAction.SelectSuit];
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
            if (isNotInteractable) return;
            Toggle();
        }

        #endregion

        #region Controller events

        public void OnSelect(BaseEventData eventData)
        {
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
            Toggle();
        }

        #endregion
    }
}
