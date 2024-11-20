using LethalInternship.Constants;
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
            return $"InternID: {InternID}, VoiceFolder: {VoiceFolder}, VoicePitch {VoicePitch}, CurrentAudioSource : {CurrentAudioSource?.name}";
        }

        public void SetCooldownAudio(float cooldown)
        {
            cooldownPlayAudio = cooldown;
        }

        public void SetNewRandomCooldownAudio()
        {
            Random randomInstance = new Random();
            cooldownPlayAudio = (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE, VoicesConst.MAX_COOLDOWN_PLAYVOICE);
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

        public bool IsTalking()
        {
            return CurrentAudioSource.isPlaying || aboutToTalk;
        }

        public void PlayRandomVoiceAudio(EnumVoicesState enumVoicesState)
        {
            aboutToTalk = false;
            string audioClipPath = GetRandomAudioClipByState(enumVoicesState);
            if (!string.IsNullOrWhiteSpace(audioClipPath))
            {
                // Can take time, coroutine stuff
                AudioManager.Instance.SyncPlayAudio(audioClipPath, InternID);
                aboutToTalk = true;
            }
        }

        public void PlayAudioClip(AudioClip audioClip)
        {
            CurrentAudioSource.volume = VoicesConst.VOLUME_INTERNS;
            CurrentAudioSource.pitch = VoicePitch;

            CurrentAudioSource.clip = audioClip;
            AudioManager.Instance.FadeInAudio(CurrentAudioSource, VoicesConst.FADE_IN_TIME, VoicesConst.VOLUME_INTERNS);
            Random randomInstance = new Random();
            SetCooldownAudio(audioClip.length + (float)randomInstance.Next(VoicesConst.MIN_COOLDOWN_PLAYVOICE, VoicesConst.MAX_COOLDOWN_PLAYVOICE));

            aboutToTalk = false;
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
            return audioClipPath;
        }

        private string[] LoadAudioClipPathsByState(EnumVoicesState enumVoicesState)
        {
            string path = VoiceFolder + "\\" + enumVoicesState.ToString();

            var audioClipPaths = AudioManager.Instance.DictAudioClipsByPath
                                    .Where(x => x.Key.Contains(path));

            if (!Plugin.Config.AllowSwearing.Value)
            {
                audioClipPaths = audioClipPaths.Where(x => !x.Key.Contains(VoicesConst.SWEAR_KEYWORD));
            }

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
}
