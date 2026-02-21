using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        void AddInternsControlTip(HUDManager hudManager);

        public void AttachUIToLocalPlayer(PlayerControllerB player);

        void InitUI(Transform HUDContainerParent);

        void UpdateCurrentPointedIntern(ulong pointedInternCliendId);

        void UpdateCursorTooltipsPointingIntern(IInternAI intern);
    }
}
