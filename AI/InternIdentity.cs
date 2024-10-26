using LethalInternship.Enums;
using LethalInternship.VoiceAdapter;
using UnityEngine;

namespace LethalInternship.AI
{
    internal class InternIdentity
    {
        public int IdIdentity { get; }
        public string Name { get; set; }
        public int SuitID { get; set; }
        public InternVoice Voice { get; set; }

        public InternIdentity(int idIdentity, string name, int suitID, InternVoice voice)
        {
            IdIdentity = idIdentity;
            Name = name;
            SuitID = suitID;
            Voice = voice;
        }

        public AudioClip? GetRandomAudioClipByState(EnumAIStates enumAIState)
        {
            return Voice.GetRandomAudioClipByState(Name, enumAIState);
        }
    }
}
