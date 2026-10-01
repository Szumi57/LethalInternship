using UnityEngine;

namespace LethalInternship.Core.VoiceAdapter
{
    public class VoiceSource
    {
        public string Name { get; }
        public string? FilePath { get; }
        public AudioClip? Clip { get; set; }

        public VoiceSource(string name, string filePath)
        {
            Name = name;
            FilePath = filePath;
        }

        public VoiceSource(string name, AudioClip clip)
        {
            Name = name;
            Clip = clip;
        }
    }
}
