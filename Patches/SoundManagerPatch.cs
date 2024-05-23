using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(SoundManager))]
    internal class SoundManagerPatch
    {
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
