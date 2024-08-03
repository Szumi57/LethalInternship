using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using ModelReplacement.Monobehaviors;

namespace LethalInternship.Patches.ModPatches
{
    [HarmonyPatch(typeof(MoreCompanyCosmeticManager))]
    internal class MoreCompanyCosmeticManagerPatch
    {
        [HarmonyPatch("DangerousRenderCosmetics")]
        [HarmonyPrefix]
        static bool DangerousRenderCosmetics_Prefix(PlayerControllerB ___controller)
        {
            if (InternManager.Instance.IsPlayerIntern(___controller))
            {
                return false;
            }
            return true;
        }
    }
}
