using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(SoundManager))]
    internal class SoundManagerPatch
    {
        static readonly MethodInfo IndexBeginOfInternsMethod = SymbolExtensions.GetMethodInfo(() => PatchesUtil.IndexBeginOfInterns());

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
                codes[startIndex + 2].operand = IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.SoundManagerPatch.SetPlayerVoiceFilters_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

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
