using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    internal class StunGrenadeItemPatch
    {
        [HarmonyPatch("SetControlTipForGrenade")]
        [HarmonyPrefix]
        static bool SetControlTipForGrenade_PreFix(StunGrenadeItem __instance)
        {
            return !InternManager.IsObjectHeldByIntern(__instance);
        }
    }
}
