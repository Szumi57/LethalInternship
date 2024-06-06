using HarmonyLib;
using LethalInternship.Managers;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    internal class StunGrenadeItemPatch
    {
        [HarmonyPatch("SetControlTipForGrenade")]
        [HarmonyPrefix]
        static bool SetControlTipForGrenade_PreFix(StunGrenadeItem __instance)
        {
            return !InternManager.Instance.IsObjectHeldByIntern(__instance);
        }
    }
}
