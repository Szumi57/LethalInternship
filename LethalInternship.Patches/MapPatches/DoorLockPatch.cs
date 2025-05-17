using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.MapPatches
{
    /// <summary>
    /// Patch for <c>DoorLock</c>
    /// </summary>
    [HarmonyPatch(typeof(DoorLock))]
    public class DoorLockPatch
    {
        /// <summary>
        /// Patch for making the intern only open door if not locked or already opened
        /// </summary>
        /// <remarks>
        /// Needed a prefix patch for accessing the private field <c>isLocked</c> and <c>isDoorOpened</c>
        /// </remarks>
        /// <param name="__instance"></param>
        /// <param name="___isLocked"></param>
        /// <param name="___isDoorOpened"></param>
        /// <param name="playerWhoTriggered"></param>
        /// <returns></returns>
        [HarmonyPatch("OpenOrCloseDoor")]
        [HarmonyPrefix]
        static bool OpenOrCloseDoor_PreFix(DoorLock __instance,
                                           bool ___isLocked,
                                           bool ___isDoorOpened,
                                           PlayerControllerB playerWhoTriggered)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAIIfLocalIsOwner((int)playerWhoTriggered.playerClientId);
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
