using LethalInternship.Core.Managers;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class SuitMenu : MonoBehaviour
    {
        private SuitListPanel SuitListPanel = null!;

        void Awake()
        {
            SuitListPanel = GetComponentInChildren<SuitListPanel>();
        }

        public void MouseOver()
        {
            if (UIManager.Instance.IsCommandsAllOpened)
                UIManager.Instance.CommandsAllController.SetOnlyListInternsAndSuitCommandsVisible();
        }

        public void MouseLeave()
        {
            if (SuitListPanel != null
                && SuitListPanel.isActiveAndEnabled
                && SuitListPanel.Hovering)
            {
                return;
            }

            if (UIManager.Instance.IsCommandsAllOpened)
                UIManager.Instance.CommandsAllController.SetAllVisible();
        }
    }
}
