using LethalInternship.Core.VoiceAdapter;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace LethalInternship.Core.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance = null!;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(AudioManager));
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public Dictionary<string, VoiceSource> Voices = new Dictionary<string, VoiceSource>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        public void Init()
        {
            PluginLoggerHook.LogInfo?.Invoke("Audio manager : Loading assets...");
            try
            {
                LoadAllVoiceLanguageAudioAssets();
            }
            catch (Exception ex)
            {
                PluginLoggerHook.LogError?.Invoke($"Error while loading voice audios, error : {ex.Message}");
            }
        }

        private void LoadAllVoiceLanguageAudioAssets()
        {
            // Try to load user custom voices
            string directoryPath = Path.Combine(PluginRuntimeProvider.Context.DirectoryName, VoicesConst.VOICES_PATH);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string[] customVoiceFiles = Directory.GetFiles(directoryPath, "*.ogg", SearchOption.AllDirectories);

            // Load custom voices paths
            if (customVoiceFiles.Length > 0)
            {
                foreach (string filePath in customVoiceFiles)
                {
                    string customVoiceName = Path.GetFileNameWithoutExtension(filePath);

                    Voices[customVoiceName] = new VoiceSource(customVoiceName, filePath);
                }
                return; // only custom if exist
            }

            // Default voices in bundle
            foreach (AudioClip clip in PluginRuntimeProvider.Context.DefaultVoicesClips)
            {
                Voices.TryAdd(clip.name, new VoiceSource(clip.name, clip));
            }
        }

        public string FormatAudioDirectoriesNames(string directoryName)
        {
            return directoryName.Replace(" ", "").Replace("_", "").ToLower();
        }

        public void SyncPlayAudio(string clipName, int internID)
        {
            InternManager.Instance.SyncPlayAudioIntern(internID, clipName);
        }

        public void LoadAudio(string voiceName, Action<AudioClip?> callback)
        {
            if (!Voices.TryGetValue(voiceName, out VoiceSource? source))
            {
                PluginLoggerHook.LogWarning?.Invoke($"Could not find \"{voiceName}\" in loaded voices !");

                callback(null);
                return;
            }

            if (source.Clip != null)
            {
                callback(source.Clip);
                return;
            }

            StartCoroutine(LoadAudio(source, callback));
        }

        private IEnumerator LoadAudio(VoiceSource voiceSource, Action<AudioClip?> callback)
        {
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(voiceSource.FilePath, AudioType.OGGVORBIS);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                PluginLoggerHook.LogError?.Invoke($"Error while loading audio file at {voiceSource.FilePath} : {www.error}");

                callback(null);
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            voiceSource.Clip = audioClip;

            callback(audioClip);
        }

        public void FadeInAudio(AudioSource audioSource, float fadeTime, float volumeMax)
        {
            if (StartOfRound.Instance.localPlayerController.isPlayerDead)
            {
                volumeMax *= 0.8f;
            }

            StartCoroutine(FadeInAudioCoroutine(audioSource, fadeTime, volumeMax));
        }

        private IEnumerator FadeInAudioCoroutine(AudioSource audioSource, float fadeTime, float volumeMax)
        {
            if (audioSource == null)
            {
                yield break;
            }

            // https://discussions.unity.com/t/fade-out-audio-source/585912/6
            float startVolume = 0.2f;
            audioSource.volume = 0;
            audioSource.Play();

            while (audioSource.volume < volumeMax)
            {
                audioSource.volume += startVolume * Time.deltaTime / fadeTime;

                yield return null;
            }

            audioSource.volume = volumeMax;
        }

        public void FadeOutAndStopAudio(AudioSource audioSource, float fadeTime)
        {
            StartCoroutine(FadeOutAndStopAudioCoroutine(audioSource, fadeTime));
        }

        private IEnumerator FadeOutAndStopAudioCoroutine(AudioSource audioSource, float fadeTime)
        {
            if (audioSource == null
                || !audioSource.isPlaying)
            {
                yield break;
            }

            // https://discussions.unity.com/t/fade-out-audio-source/585912/6
            float startVolume = audioSource.volume;

            while (audioSource.volume > 0)
            {
                if (audioSource == null
                    || !audioSource.isPlaying)
                {
                    yield break;
                }

                audioSource.volume -= startVolume * Time.deltaTime / fadeTime;

                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
        }
    }
}