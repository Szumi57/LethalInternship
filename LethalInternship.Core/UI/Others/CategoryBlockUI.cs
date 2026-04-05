using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public class CategoryBlockUI : MonoBehaviour
    {
        public TextMeshProUGUI CategoryTitle = null!;

        private EnumCategoryTypeUI categoryTypeUI = EnumCategoryTypeUI.None;

        void OnEnable()
        {
            CategoryTitle.font = UIManager.Instance.FontToUse;
        }

        public void SetCategory(EnumCategoryTypeUI categoryTypeUI)
        {
            this.categoryTypeUI = categoryTypeUI;
        }

        public void UpdateTitleText(string text)
        {
            CategoryTitle.text = text;
        }
    }
}
