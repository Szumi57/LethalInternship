using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.TooltipBar;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class ButtonSelectSuit : MonoBehaviour
    {
        public static System.Action<int> OnSuitSelected = null!;

        public GameObject SuitListPanel = null!;
        public Transform SuitListContentTransform = null!;

        public GameObject SuitButtonPrefab = null!;

        public Image FrameImage = null!;
        public Image IconImage = null!;

        public bool IsNotAvailable;

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

        public void Selected()
        {
            Toggle();
        }

        public void MouseOver()
        {
            TooltipBarUI.Instance.RequestShow("Select Suit test");

            if (IsNotAvailable) return;

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            TooltipBarUI.Instance.Hide();

            if (IsNotAvailable) return;

            SetButtonNotHovered();
        }
    }
}
