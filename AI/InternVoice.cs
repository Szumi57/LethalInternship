using LethalInternship.Constants;
using LethalInternship.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using AudioManager = LethalInternship.Managers.AudioManager;
using Random = System.Random;

namespace LethalInternship.AI
{
    internal class InternVoice
    {
        public int InternID { get; set; }
        public string VoiceFolder { get; set; }
        public float VoicePitch { get; set; }
        public AudioSource CurrentAudioSource { get; set; } = null!;

        private float cooldownPlayAudio = 0f;
        private bool aboutToTalk;

        private Dictionary<EnumVoicesState, List<string>> dictAvailableAudioClipPathsByState = new Dictionary<EnumVoicesState, List<string>>();
        private Dictionary<EnumVoicesState, List<string>> availableAudioClipPaths = new Dictionary<EnumVoicesState, List<string>>();

        public InternVoice(string voiceFolder, float voicePitch)
        {
            this.VoiceFolder = voiceFolder;
            this.VoicePitch = voicePitch;
        }

        public override string ToString()
        {
            return $"InternID: {InternID}, VoiceFolder: {VoiceFolder}, VoicePitch {VoicePitch}";
        }

        public void SetCooldownAudio(float cooldown)
        {
            cooldownPlayAudio = cooldown;
        }

        public void SetNewRandomCooldownAudio()
        {
            cooldownPlayAudio = GetRandomCooldown();
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

        public bool CanPlayAudioAfterCooldown()
        {
            return cooldownPlayAudio == 0f;
        }

        public bool IsTalking()
        {
            return CurrentAudioSource.isPlaying || aboutToTalk;
        }

        public void PlayRandomVoiceAudio(EnumVoicesState enumVoicesState, PlayVoiceParameters parameters)
        {
            ResetAboutToTalk();
            string audioClipPath = GetRandomAudioClipByState(enumVoicesState, parameters.IsInternInside);
            if (string.IsNullOrWhiteSpace(audioClipPath))
            {
                return;
            }

            aboutToTalk = true;
            if (parameters.ShouldSync)
            {
                // Can take time, coroutine stuff
                AudioManager.Instance.SyncPlayAudio(audioClipPath, InternID);
            }
            else
            {
                AudioManager.Instance.PlayAudio(audioClipPath, this);
            }
        }

        public void PlayAudioClip(AudioClip audioClip)
        {
            ResetAboutToTalk();

            CurrentAudioSource.pitch = VoicePitch;
            CurrentAudioSource.clip = audioClip;
            AudioManager.Instance.FadeInAudio(CurrentAudioSource, VoicesConst.FADE_IN_TIME, Plugin.Config.GetVolumeInterns());

            SetCooldownAudio(audioClip.length + GetRandomCooldown());
        }

        private float GetRandomCooldown()
        {
            // Set random cooldown
            Random randomInstance = new Random();
            switch (Plugin.Config.Talkativeness.Value)
            {
                case (int)EnumTalkativeness.Shy:
                    return (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_SHY, VoicesConst.MAX_COOLDOWN_PLAYVOICE_SHY);
                case (int)EnumTalkativeness.Normal:
                    return (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_NORMAL, VoicesConst.MAX_COOLDOWN_PLAYVOICE_NORMAL);
                case (int)EnumTalkativeness.Talkative:
                    return (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_TALKATIVE, VoicesConst.MAX_COOLDOWN_PLAYVOICE_TALKATIVE);
                case (int)EnumTalkativeness.CantStopTalking:
                    return (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_CANTSTOPTALKING, VoicesConst.MAX_COOLDOWN_PLAYVOICE_CANTSTOPTALKING);
                default:
                    return 0f;
            }
        }

        public void ResetAboutToTalk()
        {
            aboutToTalk = false;
        }

        private string GetRandomAudioClipByState(EnumVoicesState enumVoicesState,
                                                 bool isInternInside)
        {
            if (!dictAvailableAudioClipPathsByState.ContainsKey(enumVoicesState))
            {
                dictAvailableAudioClipPathsByState.Add(enumVoicesState, LoadAudioClipPathsByState(enumVoicesState).ToList());
            }

            if (!availableAudioClipPaths.ContainsKey(enumVoicesState))
            {
                availableAudioClipPaths.Add(enumVoicesState, FilterAudioClipPaths(dictAvailableAudioClipPathsByState[enumVoicesState], isInternInside).ToList());
            }

            if (availableAudioClipPaths[enumVoicesState].Count == 0)
            {
                availableAudioClipPaths[enumVoicesState] = FilterAudioClipPaths(dictAvailableAudioClipPathsByState[enumVoicesState], isInternInside).ToList();
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
            return audioClipPath;
        }

        private IEnumerable<string> FilterAudioClipPaths(List<string> audioClipPaths,
                                                         bool isInternInside)
        {
            var query = audioClipPaths.AsEnumerable();

            if (!Plugin.Config.AllowSwearing.Value)
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.SWEAR_KEYWORD.ToLower()));
            }

            if (isInternInside)
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.OUTSIDE_KEYWORD.ToLower()));
            }
            else
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.INSIDE_KEYWORD.ToLower()));
            }

            return query;
        }

        private string[] LoadAudioClipPathsByState(EnumVoicesState enumVoicesState)
        {
            string path = string.Join(' ', VoiceFolder + "\\" + enumVoicesState.ToString()).Replace("_", "").ToLower();

            var audioClipPaths = AudioManager.Instance.DictAudioClipsByPath
                                    .Where(x => x.Key.Replace(" ", "").Replace("_", "").ToLower().Contains(path));

            Plugin.LogDebug($"Loaded {audioClipPaths.Count()} path containing {path}");
            return audioClipPaths.Select(y => y.Key).ToArray();
        }

        public void ResetAvailableAudioPaths()
        {
            dictAvailableAudioClipPathsByState.Clear();
            availableAudioClipPaths.Clear();
        }

        public void StopAudioFadeOut()
        {
            AudioManager.Instance.FadeOutAndStopAudio(CurrentAudioSource, VoicesConst.FADE_OUT_TIME);
        }
    }

    public struct PlayVoiceParameters
    {
        public bool ShouldSync { get; set; }
        public bool IsInternInside { get; set; }
    }
}
