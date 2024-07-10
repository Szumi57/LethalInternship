using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using NetworkManager = Unity.Netcode.NetworkManager;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>SoundManager</c>
    /// </summary>
    [HarmonyPatch(typeof(SoundManager))]
    internal class SoundManagerPatch
    {
        /// <summary>
        /// Patch for only set player pitch for not intern
        /// </summary>
        /// <param name="playerObjNum"></param>
        /// <returns></returns>
        [HarmonyPatch("SetPlayerPitch")]
        [HarmonyPrefix]
        static bool SetPlayerPitch_PreFix(int playerObjNum)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI(playerObjNum);
            if (internAI != null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Bypass the debug log if local player null, for less annoying debug logs
        /// </summary>
        /// <param name="___localPlayer"></param>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(ref PlayerControllerB ___localPlayer)
        {
            ___localPlayer = GameNetworkManager.Instance.localPlayerController;
            if( ___localPlayer == null || NetworkManager.Singleton == null) 
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Set the player voice filter only for irl players not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("SetPlayerVoiceFilters")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SetPlayerVoiceFilters_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()" //128
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL"
                    && codes[i + 5].ToString() == "ret NULL") // 133
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
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.SoundManagerPatch.SetPlayerVoiceFilters_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Initialize arrays with for the right amount of entities (player + interns)
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(SoundManager __instance)
        {
            int playersAndInternsCount = StartOfRound.Instance.allPlayerObjects.Length;

            __instance.playerVoicePitchLerpSpeed = new float[playersAndInternsCount];
            Array.Fill(__instance.playerVoicePitchLerpSpeed, 3f);

            __instance.playerVoicePitchTargets = new float[playersAndInternsCount];
            Array.Fill(__instance.playerVoicePitchTargets, 1f);

            __instance.playerVoicePitches = new float[playersAndInternsCount];
            Array.Fill(__instance.playerVoicePitches, 1f);

            __instance.playerVoiceVolumes = new float[playersAndInternsCount];
            Array.Fill(__instance.playerVoiceVolumes, 0.5f);
        }
    }
}
