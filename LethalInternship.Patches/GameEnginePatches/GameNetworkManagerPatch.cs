﻿using HarmonyLib;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>GameNetworkManager</c>
    /// </summary>
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        /// <summary>
        /// Patch to intercept when saving base game, save our also plugin 
        /// </summary>
        [HarmonyPatch("SaveGame")]
        [HarmonyPostfix]
        public static void SaveGame_Postfix()
        {
            SaveManagerProvider.Instance.SavePluginInfos();
        }
    }
}
