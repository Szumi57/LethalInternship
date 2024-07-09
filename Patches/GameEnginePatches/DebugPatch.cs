using HarmonyLib;
using System;
using UnityEngine;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for the debug system
    /// </summary>
    [HarmonyPatch(typeof(Debug))]
    internal class DebugPatch
    {
        /// <summary>
        /// Intercept log error to log more info, i.e. the stack trace not always shown
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("LogError", new Type[] { typeof(object) })]
        [HarmonyPrefix]
        public static bool LogError_Prefix()
        {
            Plugin.Logger.LogDebug(Environment.StackTrace);
            return true;
        }
    }
}
