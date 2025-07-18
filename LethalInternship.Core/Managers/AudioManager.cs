using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace LethalInternship.Core.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; } = null!;

        public Dictionary<string, AudioClip?> DictAudioClipsByPath = new Dictionary<string, AudioClip?>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;

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
            string folderPath = PluginRuntimeProvider.Context.VoicesPath;
            if (Directory.Exists(folderPath))
            {
                // Load all paths
                foreach (string filePath in Directory.GetFiles(folderPath, "*.ogg", SearchOption.AllDirectories))
                {
                    AddPath("file://" + filePath);
                }

                return;
            }

            // Try to load decompress default voices
            folderPath = Path.Combine(PluginRuntimeProvider.Context.DirectoryName, VoicesConst.VOICES_PATH);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);

                // Load zip and extract it
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Assets.Audio.Voices.DefaultVoices.zip"))
                {
                    using (ZipArchive archive = new ZipArchive(resource, ZipArchiveMode.Read))
                    {
                        // Works if using 7zip to re-zip archive from dropbox (extract and rezip), why ?
                        archive.ExtractToDirectory(folderPath);
                    }
                }
            }

            // Load all paths
            foreach (string filePath in Directory.GetFiles(folderPath, "*.ogg", SearchOption.AllDirectories))
            {
                AddPath("file://" + filePath);
            }
        }

        private void AddPath(string path)
        {
            if (DictAudioClipsByPath == null)
            {
                DictAudioClipsByPath = new Dictionary<string, AudioClip?>();
            }

            if (DictAudioClipsByPath.ContainsKey(path))
            {
                PluginLoggerHook.LogWarning?.Invoke($"A path of the same has already been added, path {path}");
            }
            else
            {

                DictAudioClipsByPath.Add(path, null);
            }
        }

        public void SyncPlayAudio(string path, int internID)
        {
            string smallPath = string.Empty;

            try
            {
                int indexOfSmallPath = path.IndexOf(VoicesConst.VOICES_PATH);
                smallPath = path.Substring(indexOfSmallPath);
            }
            catch (Exception ex)
            {
                PluginLoggerHook.LogError?.Invoke($"Error while loading voice audios, error : {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(smallPath))
            {
                PluginLoggerHook.LogError?.Invoke($"Problem occured while getting the small path of audio clip, original path : {path}");
                return;
            }

            InternManager.Instance.SyncPlayAudioIntern(internID, smallPath);
        }

        public void PlayAudio(string smallPathAudioClip, IInternVoice internVoice)
        {
            var audioClipByPath = DictAudioClipsByPath.FirstOrDefault(x => x.Key.Contains(smallPathAudioClip));
            AudioClip? audioClip = audioClipByPath.Value;
            if (audioClip == null)
            {
                StartCoroutine(LoadAudioAndPlay(audioClipByPath.Key, internVoice));
            }
            else
            {
                internVoice.PlayAudioClip(audioClip);
            }
            PluginLoggerHook.LogDebug?.Invoke($"New audioClip loaded {smallPathAudioClip}");
        }

        private IEnumerator LoadAudioAndPlay(string uri, IInternVoice internVoice)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    internVoice.ResetAboutToTalk();
                    PluginLoggerHook.LogError?.Invoke($"Error while loading audio file at {uri} : {www.error}");
                }
                else
                {
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                    AddAudioClip(uri, audioClip);

                    internVoice.PlayAudioClip(audioClip);
                }
            }
        }

        private void AddAudioClip(string path, AudioClip audioClip)
        {
            if (DictAudioClipsByPath == null)
            {
                DictAudioClipsByPath = new Dictionary<string, AudioClip?>();
            }

            if (DictAudioClipsByPath.ContainsKey(path))
            {
                DictAudioClipsByPath[path] = audioClip;
            }
            else
            {
                DictAudioClipsByPath.Add(path, audioClip);
            }
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