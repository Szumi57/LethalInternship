using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace LethalInternship.Patches.GameEnginePatches
{
    [HarmonyPatch(typeof(AudioMixer))]
    internal class AudioMixerPatch
    {
        //[HarmonyPatch("SetFloat")]
        //[HarmonyPrefix]
        public static bool SetFloat_Prefix(string name, float value)
        {
            if (!name.StartsWith("PlayerVolume") && !name.StartsWith("PlayerPitch"))
            {
                return true;
            }

            string onlyNumberName = name.Replace("PlayerVolume", "").Replace("PlayerPitch", "");
            int playerObjectNumber = int.Parse(onlyNumberName);
            if (playerObjectNumber <= 3)
            {
                return true;
            }

            PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObjectNumber];
            if (playerControllerB == null)
            {
                return true;
            }

            AudioSource voiceSource = playerControllerB.currentVoiceChatAudioSource;
            if (voiceSource != null)
            {
                if (name.StartsWith("PlayerVolume"))
                {
                    voiceSource.volume = value / 16f;
                }
                else if (name.StartsWith("PlayerPitch"))
                {
                    voiceSource.pitch = value;
                }
            }
            return false;
        }
    }
}
