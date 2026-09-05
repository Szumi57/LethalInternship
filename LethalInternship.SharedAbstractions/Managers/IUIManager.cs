using TMPro;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        bool IsCommandsAllOpened { get; }
        bool IsCommandsOneOpened { get; }
        bool IsAnyMenuOpened { get; }
        bool IsAnyCommandsMenuOpenedOrWasOpened { get; }

        GameObject? LastSelectedUI { get; }

        TMP_FontAsset FontToUse { get; }

        void ShowCommandsAll();
        void HideCommandsAll(bool resetCameraFocus = true);

        void HideAll();

        void InitUI(Transform HUDContainerParent);
        void UpdateLastSelectedUI(GameObject? gameObject);
    }
}
