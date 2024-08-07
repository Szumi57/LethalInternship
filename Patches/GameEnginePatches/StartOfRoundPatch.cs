﻿using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patches for <c>StartOfRound</c>
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        /// <summary>
        /// Load the managers if the client is host/server
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void Awake_Prefix(StartOfRound __instance)
        {
            Plugin.LogDebug("Initialize managers...");

            GameObject objectManager = Object.Instantiate(PluginManager.Instance.InternManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            objectManager = Object.Instantiate(PluginManager.Instance.SaveManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            objectManager = Object.Instantiate(PluginManager.Instance.TerminalManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            Plugin.LogDebug("... Managers started");
        }

        /// <summary>
        /// Patch to intercept the end of round for managing interns
        /// </summary>
        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        static void ShipHasLeft_PreFix()
        {
            InternManager.Instance.SyncEndOfRoundInterns();
        }

        #region Transpilers

        /// <summary>
        /// Patch for only try to revive irl players not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("ReviveDeadPlayers")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReviveDeadPlayers_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //410
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.ReviveDeadPlayers_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch for sync the ship unlockable only for irl players not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("SyncShipUnlockablesClientRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SyncShipUnlockablesClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 343
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.SyncShipUnlockablesClientRpc_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch for bypassing the annoying debug logs.
        /// </summary>
        /// <remarks>
        /// Todo: check for real problems in the sound sector for interns
        /// </remarks>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("RefreshPlayerVoicePlaybackObjects")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RefreshPlayerVoicePlaybackObjects_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString().StartsWith("ldstr \"Refreshing voice playback objects. Number of voice objects found: {0}\"")//13
                    && codes[i + 6].ToString().StartsWith("call static void UnityEngine.Debug::Log(object message)")) //19
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsBypass(codes, generator, startIndex, 7);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not bypass debug log 1");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].ToString().StartsWith("ldstr \"Skipping player #{0} as they are not controlled or dead\"") //34
                    && codes[i + 5].ToString().StartsWith("br")) //39
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsBypass(codes, generator, startIndex, 6);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not bypass debug log 2");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //189
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not change limit of for loop to only real players");
            }

            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.LogDebug($"{i} {codes[i].ToString()}");
            //}

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Check only real players not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("UpdatePlayerVoiceEffects")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UpdatePlayerVoiceEffects_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //282
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.UpdatePlayerVoiceEffects_Transpiler could not change limit of for loop to only real players");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Check only real players not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("ResetShipFurniture")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ResetShipFurniture_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //176
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ResetShipFurniture_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        #endregion

        /// <summary>
        /// Patch for sync the info from the save from the server to the client (who does not load the save file)
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        static void OnPlayerConnectedClientRpc_PostFix(StartOfRound __instance)
        {
            SaveManager.Instance.SyncNbInternsOwnedServerRpc(__instance.NetworkManager.LocalClientId);
        }
    }

    //[HarmonyPatch(typeof(ShowerTrigger))] // make sure Harmony inspects the class
    //class MyPatches
    //{
    //    [HarmonyPatch("CheckBoundsForPlayers")]
    //    [HarmonyPrefix]
    //    static void Prefix(out System.Diagnostics.Stopwatch __state)
    //    {
    //        __state = System.Diagnostics.Stopwatch.StartNew();
    //    }

    //    [HarmonyPatch("CheckBoundsForPlayers")]
    //    [HarmonyPostfix]
    //    static void Postfix(System.Diagnostics.Stopwatch __state)
    //    {
    //        __state.Stop();
    //        var elapsedMs = __state.Elapsed.TotalMilliseconds;
    //        Plugin.LogDebug($"ShowerTrigger: {elapsedMs}ms");
    //    }
    //}

    //[HarmonyPatch] // make sure Harmony inspects the class
    //class MyPatches
    //{
    //    static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        var a = AccessTools.GetTypesFromAssembly(typeof(StartOfRound).Assembly)
    //            .SelectMany(type => type.GetMethods())
    //            .Where(method => //method.ReturnType != typeof(void) &&
    //                            !method.DeclaringType.Name.Contains("BaseServer`3")&&
    //                            !method.DeclaringType.Name.Contains("BaseClient`3") &&
    //                            method.Name.Contains("Update"));
    //        foreach(var method in a)
    //        {
    //            Plugin.LogDebug($"{method.DeclaringType.Name}.{method.Name}");
    //        }

    //        return AccessTools.GetTypesFromAssembly(typeof(StartOfRound).Assembly)
    //            .SelectMany(type => type.GetMethods())
    //            .Where(method => //method.ReturnType != typeof(void) &&
    //                            !method.DeclaringType.Name.Contains("BaseServer`3") && 
    //                            !method.DeclaringType.Name.Contains("BaseClient`3") &&
    //                            method.Name.Contains("Update"))
    //            .Cast<MethodBase>();
    //    }

    //    // prefix all methods in someAssembly with a non-void return type and beginning with "Player"
    //    static void Prefix(out System.Diagnostics.Stopwatch __state)
    //    {
    //        __state = System.Diagnostics.Stopwatch.StartNew();
    //    }

    //    static void Postfix(MethodBase __originalMethod, System.Diagnostics.Stopwatch __state)
    //    {
    //        __state.Stop();
    //        var elapsedMs = __state.Elapsed.TotalMilliseconds;
    //        string name = __originalMethod.FullDescription();
    //        if (!name.Contains("GrabbableObject"))
    //        {
    //            if(elapsedMs > 0.5)
    //            {
    //        Plugin.LogDebug($"Method {name}: {elapsedMs}ms");
    //            }

    //        }

    //    }
    //}
}
