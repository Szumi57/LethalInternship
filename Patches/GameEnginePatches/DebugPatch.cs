using HarmonyLib;
using System;
using UnityEngine;

namespace LethalInternship.Patches.GameEnginePatches
{
    [HarmonyPatch(typeof(Debug))]
    internal class DebugPatch
    {
        [HarmonyPatch("LogError", new Type[] { typeof(object) })]
        [HarmonyPrefix]
        public static bool LogError_Prefix()
        {
            Plugin.Logger.LogDebug(Environment.StackTrace);
            return true;
        }
    }
}
