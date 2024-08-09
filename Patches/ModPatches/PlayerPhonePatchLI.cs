using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using Scoops.misc;

namespace LethalInternship.Patches.ModPatches
{
    [HarmonyPatch(typeof(PlayerPhone))]
    internal class PlayerPhonePatchLI
    {
        [HarmonyPatch("UpdatePhoneSanity")]
        [HarmonyPrefix]
        static bool UpdatePhoneSanity_PreFix(PlayerControllerB playerController)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI != null)
            {
                return false;
            }
            return true;
        }
    }
}
