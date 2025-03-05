using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using MoreCompany;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Audio;
using NetworkManager = Unity.Netcode.NetworkManager;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>SoundManager</c>
    /// </summary>
    [HarmonyPatch(typeof(SoundManager))]
    public class SoundManagerPatch
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
            //int playersAndInternsCount = StartOfRound.Instance.allPlayerObjects.Length;

            //Array.Resize<float>(ref __instance.playerVoicePitchLerpSpeed, playersAndInternsCount);
            //Array.Resize<float>(ref __instance.playerVoicePitchTargets, playersAndInternsCount);
            //Array.Resize<float>(ref __instance.playerVoicePitches, playersAndInternsCount);
            //Array.Resize<float>(ref __instance.playerVoiceVolumes, playersAndInternsCount);

            //// From moreCompany
            //Array.Resize<AudioMixerGroup>(ref __instance.playerVoiceMixers, playersAndInternsCount);
            //AudioMixerGroup audioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault((AudioMixerGroup x) => x.name.StartsWith("VoicePlayer"));
            //for (int i = 0; i < playersAndInternsCount; i++)
            //{
            //    __instance.playerVoicePitchLerpSpeed[i] = 3f;
            //    __instance.playerVoicePitchTargets[i] = 1f;
            //    __instance.playerVoicePitches[i] = 1f;
            //    __instance.playerVoiceVolumes[i] = 0.5f;
            //    if (__instance.playerVoiceMixers[i] != null)
            //    {
            //        Plugin.LogDebug($"playerVoiceMixers {i} {audioMixerGroup}");
            //        __instance.playerVoiceMixers[i] = audioMixerGroup;
            //    }
            //}
        }
    }
}
