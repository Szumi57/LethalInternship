using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.InternBlocks
{
    public class CategoryBlockUI : MonoBehaviour
    {
        public TextMeshProUGUI CategoryTitle = null!;

        private EnumCategoryTypeUI categoryTypeUI = EnumCategoryTypeUI.TooFar;

        void OnEnable()
        {
            CategoryTitle.font = UIManager.Instance.FontToUse;
        }

        public void SetCategory(EnumCategoryTypeUI categoryTypeUI)
        {
            this.categoryTypeUI = categoryTypeUI;
        }

        public void UpdateInternCount(int internCount)
        {
            CategoryTitle.text = string.Format($"{UIConst.CATEGORIES_STRING[(int)categoryTypeUI]}", internCount);
        }
    }
}
