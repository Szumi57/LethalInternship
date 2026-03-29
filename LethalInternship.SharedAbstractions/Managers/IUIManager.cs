using GameNetcodeStuff;
using TMPro;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        TMP_FontAsset FontToUse { get; }

        void AttachUIToLocalPlayer(PlayerControllerB player);

        void ToogleAllCommands();
        void ShowAllCommands();
        void HideCommandsAll();

        void InitUI(Transform HUDContainerParent);

        void UpdateCursorTooltipsOfPointedIntern();
    }
}
