﻿using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Patches.GameEnginePatches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void Awake_Prefix(StartOfRound __instance)
        {
            Plugin.Logger.LogDebug("Initialize managers...");
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                GameObject objectManager = Object.Instantiate(PluginManager.Instance.InternManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                objectManager = Object.Instantiate(PluginManager.Instance.SaveManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                objectManager = Object.Instantiate(PluginManager.Instance.TerminalManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                Plugin.Logger.LogDebug("Managers started");
            }
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        static void ShipHasLeft_PreFix()
        {
            InternManager.Instance.SyncEndOfRoundInterns();
        }

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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.ReviveDeadPlayers_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        // todo remove log debug supression
        [HarmonyPatch("RefreshPlayerVoicePlaybackObjects")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RefreshPlayerVoicePlaybackObjects_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString() == "ldstr \"Refreshing voice playback objects. Number of voice objects found: {0}\"" //13
                    && codes[i + 6].ToString() == "call static void UnityEngine.Debug::Log(object message)") //19
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 7; i++)//20
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not remove laggy \"Refreshing voice playback objects\" debug log");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "ldstr \"Skipping player #{0} as they are not controlled or dead\"" //34
                    && codes[i + 4].ToString() == "call static void UnityEngine.Debug::Log(object message)") //38
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 5; i++)//39
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not remove laggy \"Skipping player\" debug log");
            }

            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"RefreshPlayerVoicePlaybackObjects ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"RefreshPlayerVoicePlaybackObjects ======================");
            return codes.AsEnumerable();
        }
    }
}