using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NWTWA.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(PlayerControllerB __instance)
        {
            if (__instance.playerUsername == "Intern")
            {
                return false;
            }
            //Plugin.Logger.LogDebug($"{__instance.playerUsername} player pos: {__instance.transform.position}");
            //Plugin.Logger.LogDebug($"{IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint", false).ReadValue<float>()}");
            //Plugin.Logger.LogDebug($"player {__instance.moveInputVector}");
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(ref PlayerControllerB __instance)
        {
            //if (__instance.playerUsername == "Intern")
            //{
            //    Plugin.Logger.LogDebug($"bypass for {__instance.playerUsername}");
            //    return false;
            //}
        }
    }
}
