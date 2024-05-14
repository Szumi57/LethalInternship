using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(PlayerControllerB __instance)
        {
            //todo uniqueness
            if (__instance.playerUsername == "Intern")
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("Jump_performed")]
        [HarmonyPrefix]
        static bool Jump_performed_PreFix(PlayerControllerB __instance)
        {
            Plugin.Logger.LogDebug($"{__instance.playerUsername} try to jump");
            if (__instance.playerUsername == "Intern")
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("PerformEmote")]
        [HarmonyPostfix]
        static void PerformEmote_PostFix(PlayerControllerB __instance)
        {
            if (__instance.playerUsername != "Player #0")
            {
                return;
            }

            StartOfRoundPatch.SpawnIntern(__instance.transform);
        }
    }
}
