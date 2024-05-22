using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;

namespace LethalInternship.Patches
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
            InternAI? internAI = StartOfRoundPatch.GetInternAI((int)playerWhoTriggered.playerClientId);
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
