using GameNetcodeStuff;
using HarmonyLib;
using ModelReplacement.Monobehaviors;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ModelReplacement.Patches.PlayerControllerBPatch))]
    public class ModelReplacementPlayerControllerBPatchPatch
    {
        [HarmonyPatch("StartPatch")]
        [HarmonyPrefix]
        static bool StartPatch_Prefix(PlayerControllerB __0)
        {
            if (__0.gameObject.GetComponent<MoreCompanyCosmeticManager>() != null)
            {
                return false;
            }
            return true;
        }
    }
}
