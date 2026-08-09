using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSelectSuit : MonoBehaviour, IVisibilityUI
    {
        public static System.Action<int> OnSuitSelected = null!;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.None;
        EnumUIGroups IVisibilityUI.GroupUI => this.GroupUI;

        public GameObject SuitListPanel = null!;
        public Transform SuitListContentTransform = null!;

        public GameObject SuitButtonPrefab = null!;

        public Image FrameImage = null!;
        public Image IconImage = null!;

        private float transparencyFull = 1f;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : SetTooltipMessage();

        void Awake()
        {
            Go = this.gameObject;
        }

        void OnEnable()
        {
            SetButtonNotHovered();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            isNotInteractable = !interactable;
            if (isNotInteractable)
                SetAlpha(IconImage, 0.2f);
            else
                SetAlpha(IconImage, transparencyFull);
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
            bool enable = !SuitListPanel.activeSelf;
            if (enable)
            {
                List<int> spawnedSuits = InternManager.Instance.GetListOfAvailableSuitIDs();
                if (spawnedSuits.Count > 0)
                {
                    SuitListPanel.SetActive(true);
                    Populate(spawnedSuits);
                }
            }
            else
            {
                Close();
            }
        }

        private void Close()
        {
            SuitListPanel.SetActive(false);
        }

        private void Populate(List<int> suits)
        {
            foreach (Transform c in SuitListContentTransform)
                Destroy(c.gameObject);

            foreach (var suitID in suits)
            {
                var item = Instantiate(SuitButtonPrefab, SuitListContentTransform);
                TMP_Text tMP_Text = item.GetComponentInChildren<TMP_Text>();
                tMP_Text.text = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
                tMP_Text.font = UIManager.Instance.FontToUse;

                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectSuit(suitID);
                });
            }
        }

        private void SelectSuit(int suitID)
        {
            OnSuitSelected?.Invoke(suitID);
            Close();
        }

        private void SetButtonHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 10f;
        }

        private void SetButtonNotHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 20f;
        }

        private string SetTooltipMessage()
        {
            return UIConst.COMMANDS_BUTTON_STRING[(int)EnumInputAction.SelectSuit];
        }

        public void Selected()
        {
            if (isNotInteractable) return;
            Toggle();
        }

        public void MouseOver()
        {
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);

            if (isNotInteractable) return;

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            UIManager.Instance.ToolTipBarUI.Hide();
            SetButtonNotHovered();
        }
    }
}
