using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(DoorLock))]
    internal class DoorLockPatch
    {
        [HarmonyPatch("OpenOrCloseDoor")]
        [HarmonyPrefix]
        static bool OpenOrCloseDoor_PreFix(DoorLock __instance,
                                           bool ___isLocked,
                                           bool ___isDoorOpened,
                                           PlayerControllerB playerWhoTriggered)
        {
            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)playerWhoTriggered.playerClientId);
            if (internAI?.NpcController.Npc.playerClientId != playerWhoTriggered.playerClientId)
            {
                return true;
            }

            if (___isLocked || ___isDoorOpened)
            {
                return false;
            }

            __instance.OpenDoorAsEnemy();
            return true;
        }
    }
}
