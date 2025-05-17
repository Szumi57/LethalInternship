using GameNetcodeStuff;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IUIManager
    {
        void AddInternsControlTip(HUDManager hudManager);

        public void AttachUIToLocalPlayer(PlayerControllerB player);

        public void InitUI(Transform HUDContainerParent);
    }
}
