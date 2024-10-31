using LethalInternship.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AudioManager = LethalInternship.Managers.AudioManager;
using Random = System.Random;

namespace LethalInternship.AI
{
    internal class InternVoice
    {
        public string IdentityName { get; set; }

        private Dictionary<EnumVoicesState, List<string>> dictAvailableAudioClipPathsByState = new Dictionary<EnumVoicesState, List<string>>();
        private Dictionary<EnumVoicesState, List<string>> availableAudioClipPaths = new Dictionary<EnumVoicesState, List<string>>();
        private float cooldownPlayAudio = 0f;

        public InternVoice(string identityName)
        {
            IdentityName = identityName;
        }

        public void AddCooldownAudio(float cooldown)
        {
            cooldownPlayAudio = cooldown;
        }

        public void AddRandomCooldownAudio()
        {
            Random randomInstance = new Random();
            cooldownPlayAudio = (float)randomInstance.Next(Const.MIN_COOLDOWN_PLAYVOICE, Const.MAX_COOLDOWN_PLAYVOICE);
        }

        public void ReduceCooldown(float time)
        {
            // CooldownPlayAudio
            if (cooldownPlayAudio > 0f)
            {
                cooldownPlayAudio -= time;
            }
            if (cooldownPlayAudio < 0f)
            {
                cooldownPlayAudio = 0f;
            }
        }

        public bool CanPlayAudio()
        {
            return cooldownPlayAudio == 0f;
        }

        public void PlayRandomVoiceAudio(AudioSource audioSource, EnumVoicesState enumVoicesState)
        {
            string audioClipPath = GetRandomAudioClipByState(enumVoicesState);
            if (!string.IsNullOrWhiteSpace(audioClipPath))
            {
                Plugin.LogDebug($"intern with identity {IdentityName} play state {enumVoicesState} random audio");
                AudioManager.Instance.PlayAudio(audioSource, audioClipPath, this);
            }
        }

        private string GetRandomAudioClipByState(EnumVoicesState enumVoicesState)
        {
            if (!dictAvailableAudioClipPathsByState.ContainsKey(enumVoicesState))
            {
                dictAvailableAudioClipPathsByState.Add(enumVoicesState, LoadAudioClipPathsByState(enumVoicesState).ToList());
            }

            if (!availableAudioClipPaths.ContainsKey(enumVoicesState))
            {
                availableAudioClipPaths.Add(enumVoicesState, dictAvailableAudioClipPathsByState[enumVoicesState].ToList());
            }

            if (availableAudioClipPaths[enumVoicesState].Count == 0)
            {
                availableAudioClipPaths[enumVoicesState] = dictAvailableAudioClipPathsByState[enumVoicesState].ToList();
            }

            List<string> audioClipPaths = availableAudioClipPaths[enumVoicesState];
            if (audioClipPaths.Count == 0)
            {
                return string.Empty;
            }

            string audioClipPath;
            Random randomInstance = new Random();
            int index = randomInstance.Next(0, audioClipPaths.Count);

            audioClipPath = audioClipPaths[index];
            audioClipPaths.RemoveAt(index);
            Plugin.LogDebug($"======== enumVoicesState {enumVoicesState} audioClipPaths {audioClipPaths.Count}, dictAvailableAudioClipPathsByState {dictAvailableAudioClipPathsByState[enumVoicesState].Count}");
            return audioClipPath;
        }

        private string[] LoadAudioClipPathsByState(EnumVoicesState enumVoicesState)
        {
            string path = IdentityName + "\\" + enumVoicesState.ToString();

            Plugin.LogDebug($"Loaded {AudioManager.Instance.DictAudioClipsByPath
                                        .Where(x => x.Key.Contains(path)).Select(y => y.Key).Count()} path containing {path}");

            return AudioManager.Instance.DictAudioClipsByPath
                       .Where(x => x.Key.Contains(path))
                       .Select(y => y.Key).Take(1)
                       .ToArray();
        }
    }
}
