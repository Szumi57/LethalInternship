﻿using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AudioManager = LethalInternship.Core.Managers.AudioManager;
using Random = System.Random;

namespace LethalInternship.Core.Interns
{
    public class InternVoice : IInternVoice
    {
        public int InternID { get => internID; set => internID = value; }
        public string VoiceFolder { get; set; }
        public float Volume { get; set; }
        public float VoicePitch { get; set; }
        public AudioSource CurrentAudioSource { get => currentAudioSource; set => currentAudioSource = value; }

        private int internID;
        private AudioSource currentAudioSource = null!;

        private float cooldownPlayAudio = 0f;
        private bool aboutToTalk;
        private EnumVoicesState lastVoiceState;

        private Dictionary<EnumVoicesState, List<string>> dictAvailableAudioClipPathsByState = new Dictionary<EnumVoicesState, List<string>>();
        private Dictionary<EnumVoicesState, List<string>> availableAudioClipPaths = new Dictionary<EnumVoicesState, List<string>>();

        private bool wasInside;
        private bool wasAllowedToSwear;

        private int sampleDataLength = 1024;
        private float timerUpdateAmplitudeValue = 0.1f;
        private float[] clipSampleData;
        private float clipLoudness;

        public InternVoice(string voiceFolder, float volume, float voicePitch)
        {
            VoiceFolder = voiceFolder;
            Volume = volume;
            VoicePitch = voicePitch;
            clipSampleData = new float[sampleDataLength];
        }

        public override string ToString()
        {
            return $"InternID: {InternID}, VoiceFolder: {VoiceFolder}, Volume: {Volume}, VoicePitch {VoicePitch}";
        }

        public void SetCooldownAudio(float cooldown)
        {
            cooldownPlayAudio = cooldown;
        }

        public void SetNewRandomCooldownAudio()
        {
            cooldownPlayAudio = GetRandomCooldown();
        }

        public void CountTime(float time)
        {
            ReduceCooldown(time);
            UpdateTimeUpdateAmplitudeValue(time);
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

        public void UpdateTimeUpdateAmplitudeValue(float time)
        {
            timerUpdateAmplitudeValue += time;
            if (timerUpdateAmplitudeValue >= 1f)
            {
                timerUpdateAmplitudeValue = 1f;
            }
        }

        public bool CanPlayAudioAfterCooldown()
        {
            return cooldownPlayAudio == 0f;
        }

        public bool IsTalking()
        {
            return CurrentAudioSource != null && (CurrentAudioSource.isPlaying || aboutToTalk);
        }

        public void TryPlayVoiceAudio(PlayVoiceParameters parameters)
        {
            if (PluginRuntimeProvider.Context.Config.Talkativeness == (int)EnumTalkativeness.NoTalking)
            {
                return;
            }

            if (!parameters.CanTalkIfOtherInternTalk)
            {
                if (InternManager.Instance.DidAnInternJustTalkedClose(InternID))
                {
                    SetNewRandomCooldownAudio();
                    return;
                }
            }

            if (parameters.WaitForCooldown)
            {
                if (!CanPlayAudioAfterCooldown())
                {
                    return;
                }
            }

            if (!parameters.CutCurrentVoiceStateToTalk)
            {
                if (IsTalking())
                {
                    return;
                }
            }

            if (parameters.CanRepeatVoiceState)
            {
                // Wait if already in state
                if (lastVoiceState == parameters.VoiceState
                    && IsTalking())
                {
                    // We wait for end
                    return;
                }
            }
            else
            {
                // Cannot repeat allowed, if in same state no cut talking
                if (lastVoiceState == parameters.VoiceState)
                {
                    return;
                }
            }

            PlayRandomVoiceAudio(parameters.VoiceState, parameters);
            lastVoiceState = parameters.VoiceState;
            InternManager.Instance.PlayAudibleNoiseForIntern(InternID, CurrentAudioSource.transform.position, 16f, 0.9f, 5);
        }

        public void PlayRandomVoiceAudio(EnumVoicesState enumVoicesState, PlayVoiceParameters parameters)
        {
            StopAudioFadeOut();
            ResetAboutToTalk();
            string audioClipPath = GetRandomAudioClipByState(enumVoicesState, parameters);
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
            AudioManager.Instance.FadeInAudio(CurrentAudioSource, VoicesConst.FADE_IN_TIME, Volume * PluginRuntimeProvider.Context.Config.GetVolumeVoicesMultiplierInterns());

            SetCooldownAudio(audioClip.length + GetRandomCooldown());
        }

        private float GetRandomCooldown()
        {
            // Set random cooldown
            Random randomInstance = new Random();
            switch (PluginRuntimeProvider.Context.Config.Talkativeness)
            {
                case (int)EnumTalkativeness.Shy:
                    return randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_SHY, VoicesConst.MAX_COOLDOWN_PLAYVOICE_SHY);
                case (int)EnumTalkativeness.Normal:
                    return randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_NORMAL, VoicesConst.MAX_COOLDOWN_PLAYVOICE_NORMAL);
                case (int)EnumTalkativeness.Talkative:
                    return randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_TALKATIVE, VoicesConst.MAX_COOLDOWN_PLAYVOICE_TALKATIVE);
                case (int)EnumTalkativeness.CantStopTalking:
                    return randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE_CANTSTOPTALKING, VoicesConst.MAX_COOLDOWN_PLAYVOICE_CANTSTOPTALKING);
                default:
                    return 0f;
            }
        }

        public void ResetAboutToTalk()
        {
            aboutToTalk = false;
        }

        private string GetRandomAudioClipByState(EnumVoicesState enumVoicesState,
                                                 PlayVoiceParameters parameters)
        {
            if (!dictAvailableAudioClipPathsByState.ContainsKey(enumVoicesState))
            {
                dictAvailableAudioClipPathsByState.Add(enumVoicesState, LoadAudioClipPathsByState(enumVoicesState).ToList());
            }

            if (!availableAudioClipPaths.ContainsKey(enumVoicesState))
            {
                availableAudioClipPaths.Add(enumVoicesState, FilterAudioClipPaths(dictAvailableAudioClipPathsByState[enumVoicesState], parameters).ToList());
            }

            if (availableAudioClipPaths[enumVoicesState].Count == 0)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"reset audio paths");
                availableAudioClipPaths[enumVoicesState] = FilterAudioClipPaths(dictAvailableAudioClipPathsByState[enumVoicesState], parameters).ToList();
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

            if (DidParametersChanged(parameters))
            {
                // Reset pool of audio path
                availableAudioClipPaths[enumVoicesState].Clear();
            }
            else
            {
                audioClipPaths.RemoveAt(index);
            }

            return audioClipPath;
        }

        private IEnumerable<string> FilterAudioClipPaths(List<string> audioClipPaths,
                                                         PlayVoiceParameters parameters)
        {
            var query = audioClipPaths.AsEnumerable();

            if (!parameters.AllowSwearing)
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.SWEAR_KEYWORD.ToLower()));
            }

            if (parameters.IsInternInside)
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.OUTSIDE_KEYWORD.ToLower()));
            }
            else
            {
                query = query.Where(x => !x.ToLower().Contains(VoicesConst.INSIDE_KEYWORD.ToLower()));
            }

            return query;
        }

        private bool DidParametersChanged(PlayVoiceParameters parameters)
        {
            bool parametersChanged = false;
            if (wasInside != parameters.IsInternInside)
            {
                wasInside = parameters.IsInternInside;
                parametersChanged = true;
            }
            if (wasAllowedToSwear != parameters.AllowSwearing)
            {
                wasAllowedToSwear = parameters.AllowSwearing;
                parametersChanged = true;
            }

            return parametersChanged;
        }

        private string[] LoadAudioClipPathsByState(EnumVoicesState enumVoicesState)
        {
            string path = string.Join(' ', VoiceFolder + "\\" + enumVoicesState.ToString()).Replace("_", "").ToLower();

            var audioClipPaths = AudioManager.Instance.DictAudioClipsByPath
                                    .Where(x => x.Key.Replace(" ", "").Replace("_", "").ToLower().Contains(path));

            PluginLoggerHook.LogDebug?.Invoke($"Loaded {audioClipPaths.Count()} path containing {path}");
            return audioClipPaths.Select(y => y.Key).ToArray();
        }

        public void ResetAvailableAudioPaths()
        {
            dictAvailableAudioClipPathsByState.Clear();
            availableAudioClipPaths.Clear();
        }

        public void TryStopAudioFadeOut()
        {
            if (lastVoiceState != EnumVoicesState.Hit
                && lastVoiceState != EnumVoicesState.SteppedOnTrap
                && lastVoiceState != EnumVoicesState.RunningFromMonster)
            {
                StopAudioFadeOut();
            }
        }

        public void StopAudioFadeOut()
        {
            if (CurrentAudioSource.isPlaying)
            {
                AudioManager.Instance.FadeOutAndStopAudio(CurrentAudioSource, VoicesConst.FADE_OUT_TIME);
                lastVoiceState = EnumVoicesState.None;
            }
        }

        public float GetAmplitude()
        {
            // https://discussions.unity.com/t/how-do-i-get-the-current-volume-level-amplitude-of-playing-audio-not-the-set-volume-but-how-loud-it-is/162556/2
            if (CurrentAudioSource == null
                || CurrentAudioSource.clip == null)
            {
                return clipLoudness;
            }

            if (timerUpdateAmplitudeValue < 0.1f)
            {
                return clipLoudness;
            }
            timerUpdateAmplitudeValue = 0f;

            CurrentAudioSource.clip.GetData(clipSampleData, CurrentAudioSource.timeSamples);
            clipLoudness = 0f;
            foreach (var sample in clipSampleData)
            {
                clipLoudness += Mathf.Abs(sample);
            }
            clipLoudness /= sampleDataLength;
            return clipLoudness;
        }
    }
}
