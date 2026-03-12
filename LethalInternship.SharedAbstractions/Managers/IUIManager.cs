using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Interns;
using TMPro;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        TMP_FontAsset FontToUse { get; }

        void AttachUIToLocalPlayer(PlayerControllerB player);

        IInternAI? ShowCommandsWheel();

        void InitUI(Transform HUDContainerParent);

        void UpdateCursorTooltipsOfPointedIntern();
    }
}
