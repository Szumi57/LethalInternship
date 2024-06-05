using HarmonyLib;
using LethalInternship.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void Awake_Prefix(StartOfRound __instance)
        {
            Plugin.Logger.LogDebug("Initialize TerminalManager...");
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                GameObject terminalManager = Object.Instantiate(ManagersManager.Instance.TerminalManagerPrefab);
                terminalManager.GetComponent<NetworkObject>().Spawn();
                Plugin.Logger.LogDebug("TerminalManager started");
            }
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
                Plugin.Logger.LogError($"LethalInternship.Patches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not remove laggy \"Refreshing voice playback objects\" debug log");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not remove laggy \"Skipping player\" debug log");
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
