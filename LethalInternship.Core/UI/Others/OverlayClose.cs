using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public class OverlayClose : MonoBehaviour
    {
        public void MouseClick()
        {
            UIManager.Instance.HideAll();
            CommandContextService.Instance.ExitCommandMode();
        }
    }
}
