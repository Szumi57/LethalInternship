using LethalInternship.Core.UI.Others;
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
            UIVisibilityController.Instance.SetOnlyListInternsAndSuitCommandsVisible();
        }

        public void MouseLeave()
        {
            if (SuitListPanel != null
                && SuitListPanel.isActiveAndEnabled
                && SuitListPanel.Hovering)
            {
                return;
            }

            UIVisibilityController.Instance.SetAllVisible();
        }
    }
}
