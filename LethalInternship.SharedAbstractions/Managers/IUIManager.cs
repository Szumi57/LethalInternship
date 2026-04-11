using TMPro;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        TMP_FontAsset FontToUse { get; }

        void ToogleCommandsAll();
        void ShowCommandsAll();
        void HideCommandsAll(bool resetCameraFocus = true);

        void HideAll();

        void InitUI(Transform HUDContainerParent);

        void UpdateCursorTooltipsOfPointedIntern();
    }
}
