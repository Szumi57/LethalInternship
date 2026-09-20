using TMPro;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        bool IsCommandsAllOpen { get; }
        bool IsCommandsOneOpen { get; }
        bool IsAnyMenuOpen { get; }
        bool IsAnyCommandsMenuOpenOrWasOpen { get; }

        GameObject? LastSelectedUI { get; }

        TMP_FontAsset FontToUse { get; }

        void ShowCommandsAll();
        void HideCommandsAll(bool resetCameraFocus = true);

        void HideAll();

        void InitUI(Transform HUDContainerParent);
        void UpdateLastSelectedUI(GameObject? gameObject);
    }
}
