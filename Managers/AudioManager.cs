using LethalInternship.AI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace LethalInternship.Managers
{
    internal class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; } = null!;

        public Dictionary<string, AudioClip?> DictAudioClipsByPath = new Dictionary<string, AudioClip?>();
        public string LanguageDirectory = null!;

        private void Awake()
        {
            Instance = this;
            Plugin.LogDebug("=============== awake audio manager =====================");
            LoadAllVoiceLanguageAudioAssets();
        }

        private void LoadAllVoiceLanguageAudioAssets()
        {
            LanguageDirectory = Path.Combine(Plugin.DirectoryName, "Audio\\Voices\\" + Plugin.Config.VoicesLanguageFolder.Value);
            if (!Directory.Exists(LanguageDirectory))
            {
                Plugin.LogError("No voices loaded, no directory found at : " + LanguageDirectory);
                return;
            }

            // Load all paths
            foreach (string filePath in Directory.GetFiles(LanguageDirectory, "*.ogg", SearchOption.AllDirectories))
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
                Plugin.LogWarning($"A path of the same has already been added, path {path}");
            }
            else
            {
                DictAudioClipsByPath.Add(path, null);
            }
        }

        public void PlayAudio(AudioSource audioSource, string path, InternVoice internVoice)
        {
            AudioClip? audioClip = DictAudioClipsByPath[path];
            if (audioClip == null)
            {
                StartCoroutine(LoadAudioAndPlay(audioSource, path, internVoice));
            }
            else
            {
                PlayAudioOnInternVoice(audioSource, audioClip, internVoice);
            }
        }

        private IEnumerator LoadAudioAndPlay(AudioSource audioSource, string uri, InternVoice internVoice)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Plugin.LogError($"Error while loading audio file at {uri} : {www.error}");
                }
                else
                {
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                    Plugin.LogDebug($"New audioClip loaded {audioClip.length}");
                    AddAudioClip(uri, audioClip);

                    PlayAudioOnInternVoice(audioSource, audioClip, internVoice);
                }
            }
        }

        private void PlayAudioOnInternVoice(AudioSource audioSource, AudioClip audioClip, InternVoice internVoice)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            Random randomInstance = new Random();
            internVoice.AddCooldownAudio(audioClip.length + (float)randomInstance.Next(Const.MIN_COOLDOWN_PLAYVOICE, Const.MAX_COOLDOWN_PLAYVOICE));
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
    }
}
