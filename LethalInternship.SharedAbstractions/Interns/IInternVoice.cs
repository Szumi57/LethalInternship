using LethalInternship.SharedAbstractions.Parameters;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInternVoice
    {
        public int InternID { get; set; }
        public AudioSource CurrentAudioSource { get; set; }

        void TryPlayVoiceAudio(PlayVoiceParameters parameters);
        void CountTime(float time);
        bool CanPlayAudioAfterCooldown();
        bool IsTalking();
        float GetAmplitude();
        void TryStopAudioFadeOut();
        void SetNewRandomCooldownAudio();
        void PlayAudioClip(AudioClip audioClip);
        void ResetAboutToTalk();
        void StopAudioFadeOut();
    }
}
