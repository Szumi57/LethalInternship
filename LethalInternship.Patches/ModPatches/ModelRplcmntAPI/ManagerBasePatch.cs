using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using ModelReplacement.Monobehaviors;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ManagerBase))]
    public class ManagerBasePatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_Prefix(PlayerControllerB ___controller)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)___controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            // Cut update visibility of model replacement for the duration of spawning animation
            if (internAI.AnimationCoroutineRagdollingRunning)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
