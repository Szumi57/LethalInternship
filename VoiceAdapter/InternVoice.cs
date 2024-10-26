using LethalInternship.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AudioManager = LethalInternship.Managers.AudioManager;
using Random = System.Random;

namespace LethalInternship.VoiceAdapter
{
    internal class InternVoice
    {
        private Dictionary<EnumAIStates, List<AudioClip>> dictAvailableAudioClipsByState = new Dictionary<EnumAIStates, List<AudioClip>>();

        public InternVoice() { }

        public AudioClip? GetRandomAudioClipByState(string identityName, EnumAIStates enumAIState)
        {
            List<AudioClip> availableAudioClips;
            if (!dictAvailableAudioClipsByState.ContainsKey(enumAIState))
            {
                dictAvailableAudioClipsByState.Add(enumAIState, LoadAudioClipsByState(identityName, enumAIState).ToList());
            }
            availableAudioClips = dictAvailableAudioClipsByState[enumAIState];

            if (availableAudioClips.Count == 0)
            {
                availableAudioClips.AddRange(LoadAudioClipsByState(identityName, enumAIState));
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
                availableAudioClips.AddRange(LoadAudioClipsByState(identityName, enumAIState));
                return audioClip;
            }

            Random randomInstance = new Random();
            int index = randomInstance.Next(0, availableAudioClips.Count);

            audioClip = availableAudioClips[index];
            availableAudioClips.RemoveAt(index);
            return audioClip;
        }

        private AudioClip[] LoadAudioClipsByState(string identityName, EnumAIStates enumAIState)
        {
            string path = identityName;
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
                    stateFolder = "SearchingPlayer";
                    break;
                case EnumAIStates.PlayerInCruiser:
                    stateFolder = "InCruiser";
                    break;
                case EnumAIStates.Panik:
                    stateFolder = "Panik";
                    break;
                default:
                    Plugin.LogWarning($"No audio loaded for state {enumAIState} for identity name {identityName}.");
                    return new AudioClip[0];
            }

            path += "\\" + stateFolder;

            Plugin.LogDebug($"path to search {path}");
            foreach(var a in AudioManager.Instance.DictAudioClipsByPath
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
