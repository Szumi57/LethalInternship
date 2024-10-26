using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace LethalInternship.Managers
{
    internal class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; } = null!;

        public Dictionary<string, AudioClip> DictAudioClipsByPath = new Dictionary<string, AudioClip>();
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

            StartCoroutine(LoadAllOggFiles(LanguageDirectory));
        }

        private IEnumerator LoadAllOggFiles(string languageDirectory)
        {
            string uri;
            foreach (string filePath in Directory.GetFiles(languageDirectory, "*.ogg", SearchOption.AllDirectories))
            {
                uri = "file://" + filePath;
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Plugin.LogError("Error while loading audio file : " + www.error);
                    }
                    else
                    {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        Plugin.LogDebug($"New audioClip loaded {audioClip.length}");
                        AddAudioClip(filePath, audioClip);
                    }
                }
            }
        }

        private void AddAudioClip(string path, AudioClip audioClip)
        {
            if (DictAudioClipsByPath == null)
            {
                DictAudioClipsByPath = new Dictionary<string, AudioClip>();
            }

            if (DictAudioClipsByPath.ContainsKey(path))
            {
                Plugin.LogWarning($"An audioClip of the same name same path has already been added, path {path}");
            }
            else
            {
                DictAudioClipsByPath.Add(path, audioClip);
            }
        }

        //private IEnumerator LoadAllOggFilesfdgdfgdgf(string languageDirectory)
        //{
        //    EnumAIStates enumAIState;
        //    string stateDirectory;
        //    string[] fileEntries;
        //    string uri;
        //    string currentStateDirectory;

        //    // ----------------------------
        //    enumAIState = EnumAIStates.ChillWithPlayer;
        //    currentStateDirectory = "Chill";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // -------------------------------------
        //    enumAIState = EnumAIStates.FetchingObject;
        //    currentStateDirectory = "FetchingObject";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // ---------------------------------------
        //    enumAIState = EnumAIStates.GetCloseToPlayer;
        //    currentStateDirectory = "GetCloseToPlayer";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // --------------------------------
        //    enumAIState = EnumAIStates.PlayerInCruiser;
        //    currentStateDirectory = "InCruiser";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // -------------------------------------
        //    enumAIState = EnumAIStates.JustLostPlayer;
        //    currentStateDirectory = "JustLostPlayer";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // ----------------------------
        //    enumAIState = EnumAIStates.Panik;
        //    currentStateDirectory = "Panik";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }

        //    // --------------------------------------
        //    enumAIState = EnumAIStates.SearchingForPlayer;
        //    currentStateDirectory = "SearchingPlayer";
        //    stateDirectory = Path.Combine(languageDirectory, currentStateDirectory);
        //    if (!Directory.Exists(stateDirectory))
        //    {
        //        Plugin.LogWarning($"No voices loaded for the state {currentStateDirectory}, no directory found at : {stateDirectory}");
        //    }
        //    else
        //    {
        //        fileEntries = Directory.GetFiles(stateDirectory);
        //        if (fileEntries.Length == 0)
        //        {
        //            Plugin.LogWarning($"No voice files found for the state {currentStateDirectory}, at : {stateDirectory}");
        //        }

        //        foreach (string filePath in fileEntries)
        //        {
        //            uri = "file://" + filePath;
        //            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        //            {
        //                yield return www.SendWebRequest();

        //                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //                {
        //                    Plugin.LogError("Error while loading audio file : " + www.error);
        //                }
        //                else
        //                {
        //                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        //                    Plugin.LogDebug($"New audioClip for {currentStateDirectory} {audioClip.length}");
        //                    AddAudioClip(enumAIState, audioClip);
        //                }
        //            }
        //        }
        //    }
        //}

        //private void AddAudioClipddd(EnumAIStates enumAIState, AudioClip audioClip)
        //{
        //    if (!dictAudioClipByState.ContainsKey(enumAIState))
        //    {
        //        dictAudioClipByState.Add(enumAIState, new List<AudioClip> { audioClip });
        //    }
        //    else
        //    {
        //        List<AudioClip> audioClips = dictAudioClipByState[enumAIState];
        //        if (audioClips == null)
        //        {
        //            audioClips = new List<AudioClip>();
        //        }

        //        audioClips.Add(audioClip);
        //    }
        //}
    }
}
