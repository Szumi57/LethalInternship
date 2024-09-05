using HarmonyLib;
using MoreCompany;

namespace LethalInternship.Patches.ModPatches.MoreCompany
{
    [HarmonyPatch(typeof(LookForPlayersForestGiantPatch))]
    internal class LookForPlayersForestGiantPatchPatch
    {
        [HarmonyPatch("Prefix")]
        [HarmonyPrefix]
        static bool Prefix_Prefix()
        {
            return false;
        }
    }
}
