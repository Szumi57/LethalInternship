using LethalInternship.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class SuitMenu : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private SuitListPanel SuitListPanel = null!;

        void Awake()
        {
            SuitListPanel = GetComponentInChildren<SuitListPanel>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (UIManager.Instance.IsCommandsAllOpened)
                UIManager.Instance.CommandsAllController.SetOnlyListInternsAndSuitCommandsVisible();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (SuitListPanel != null
                && SuitListPanel.isActiveAndEnabled
                && SuitListPanel.Focusing)
            {
                return;
            }

            if (UIManager.Instance.IsCommandsAllOpened)
                UIManager.Instance.CommandsAllController.SetAllVisible();
        }
    }
}
