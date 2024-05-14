using LethalInternship.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class maePatch
    {
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIInterval_PreFix(MaskedPlayerEnemy __instance)
        {
            //PropertiesAndFieldsUtils.ListPropertiesAndFields(__instance.searchForPlayers);
            //Plugin.Logger.LogDebug($"{IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint", false).ReadValue<float>()}");
            //Plugin.Logger.LogDebug($"player {__instance.moveInputVector}");
            return true;
        }
    }
}
