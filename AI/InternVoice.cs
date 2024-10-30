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

        private Dictionary<EnumAIStates, List<AudioClip>> dictAvailableAudioClipsByState = new Dictionary<EnumAIStates, List<AudioClip>>();
        private float cooldownPlayAudio = 0f;

        public InternVoice(string identityName)
        {
            IdentityName = identityName;
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

        public void PlayRandomVoiceAudio(AudioSource audioSource, EnumAIStates enumAIState)
        {
            AudioClip? audioClip = GetRandomAudioClipByState(enumAIState);
            if (audioClip != null)
            {
                Random randomInstance = new Random();
                cooldownPlayAudio = audioClip.length + (float)randomInstance.Next(Const.MIN_COOLDOWN_PLAYVOICE, Const.MAX_COOLDOWN_PLAYVOICE);

                Plugin.LogDebug($"intern with identity {IdentityName} play state {enumAIState} random audio : length {audioClip.length}");
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }

        public void AddRandomCooldownAudio()
        {
            Random randomInstance = new Random();
            cooldownPlayAudio = (float)randomInstance.Next(Const.MIN_COOLDOWN_PLAYVOICE, Const.MAX_COOLDOWN_PLAYVOICE);
        }

        private AudioClip? GetRandomAudioClipByState(EnumAIStates enumAIState)
        {
            List<AudioClip> availableAudioClips;
            if (!dictAvailableAudioClipsByState.ContainsKey(enumAIState))
            {
                dictAvailableAudioClipsByState.Add(enumAIState, LoadAudioClipsByState(enumAIState).ToList());
            }
            availableAudioClips = dictAvailableAudioClipsByState[enumAIState];

            if (availableAudioClips.Count == 0)
            {
                availableAudioClips.AddRange(LoadAudioClipsByState(enumAIState));
            }

            if (availableAudioClips.Count == 0)
            {
                return null;
            }

            AudioClip audioClip;
            if (availableAudioClips.Count == 1)
            {
                audioClip = availableAudioClips[0];
                availableAudioClips.RemoveAt(0);
                availableAudioClips.AddRange(LoadAudioClipsByState(enumAIState));
                return audioClip;
            }

            Random randomInstance = new Random();
            int index = randomInstance.Next(0, availableAudioClips.Count);

            audioClip = availableAudioClips[index];
            availableAudioClips.RemoveAt(index);
            return audioClip;
        }

        private AudioClip[] LoadAudioClipsByState(EnumAIStates enumAIState)
        {
            string path = IdentityName;
            string stateFolder;
            switch (enumAIState)
            {
                case EnumAIStates.SearchingForPlayer:
                    stateFolder = "SearchingPlayer";
                    break;
                case EnumAIStates.GetCloseToPlayer:
                    stateFolder = "GetCloseToPlayer";
                    break;
                case EnumAIStates.JustLostPlayer:
                    stateFolder = "JustLostPlayer";
                    break;
                case EnumAIStates.ChillWithPlayer:
                    stateFolder = "Chill";
                    break;
                case EnumAIStates.FetchingObject:
                    stateFolder = "FetchingObject";
                    break;
                case EnumAIStates.PlayerInCruiser:
                    stateFolder = "InCruiser";
                    break;
                case EnumAIStates.Panik:
                    stateFolder = "Panik";
                    break;
                default:
                    Plugin.LogWarning($"No audio loaded for state {enumAIState} for identity name {IdentityName}.");
                    return new AudioClip[0];
            }

            path += "\\" + stateFolder;

            Plugin.LogDebug($"path to search {path}");
            foreach (var a in AudioManager.Instance.DictAudioClipsByPath
                       .Where(x => x.Key.Contains(path)))
            {
                Plugin.LogDebug($"path to search {a.ToString()}");
            }

            return AudioManager.Instance.DictAudioClipsByPath
                       .Where(x => x.Key.Contains(path))
                       .Select(y => y.Value)
                       .ToArray();
        }
    }
}
