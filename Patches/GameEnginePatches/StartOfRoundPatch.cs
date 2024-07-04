using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                Plugin.Logger.LogDebug("Initialize managers...");

                GameObject objectManager = Object.Instantiate(PluginManager.Instance.InternManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                objectManager = Object.Instantiate(PluginManager.Instance.SaveManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                objectManager = Object.Instantiate(PluginManager.Instance.TerminalManagerPrefab);
                objectManager.GetComponent<NetworkObject>().Spawn();

                Plugin.Logger.LogDebug("... Managers started");
            }
            else
            {
                Plugin.Logger.LogDebug("Client does not initialize managers.");
            }
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        static void ShipHasLeft_PreFix()
        {
            InternManager.Instance.SyncEndOfRoundInterns();
        }

        #region Transpilers

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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.SyncShipUnlockablesClientRpc_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not bypass debug log 1");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not bypass debug log 2");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.RefreshPlayerVoicePlaybackObjects could not change limit of for loop to only real players");
            }

            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}

            return codes.AsEnumerable();
        }

        [HarmonyPatch("UpdatePlayerVoiceEffects")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UpdatePlayerVoiceEffects_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}

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
                Plugin.Logger.LogError($"LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch.UpdatePlayerVoiceEffects_Transpiler could not change limit of for loop to only real players");
            }

            return codes.AsEnumerable();
        }

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
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ResetShipFurniture_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        #endregion

        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        static void OnPlayerConnectedClientRpc_PostFix(StartOfRound __instance)
        {
            SaveManager.Instance.SyncNbInternsOwnedServerRpc(__instance.NetworkManager.LocalClientId);
        }
    }
}
