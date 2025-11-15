using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public AudioSource InternVoice = null!;

        #region Voices

        public void UpdateInternVoiceEffects()
        {
            PlayerControllerB internController = NpcController.Npc;
            int internPlayerClientID = (int)internController.playerClientId;
            PlayerControllerB spectatedPlayerScript;
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
            {
                spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
            }
            else
            {
                spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController;
            }

            bool walkieTalkie = internController.speakingToWalkieTalkie
                                && spectatedPlayerScript.holdingWalkieTalkie
                                && internController != spectatedPlayerScript;

            AudioLowPassFilter audioLowPassFilter = NpcController.AudioLowPassFilterComponent;
            OccludeAudio occludeAudio = NpcController.OccludeAudioComponent;
            audioLowPassFilter.enabled = true;
            occludeAudio.overridingLowPass = walkieTalkie || internController.voiceMuffledByEnemy;
            NpcController.AudioHighPassFilterComponent.enabled = walkieTalkie;
            if (!walkieTalkie)
            {
                creatureVoice.spatialBlend = 1f;
                creatureVoice.bypassListenerEffects = false;
                creatureVoice.bypassEffects = false;
                creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                audioLowPassFilter.lowpassResonanceQ = 1f;
            }
            else
            {
                creatureVoice.spatialBlend = 0f;
                if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                {
                    creatureVoice.panStereo = 0f;
                    creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                    creatureVoice.bypassListenerEffects = false;
                    creatureVoice.bypassEffects = false;
                }
                else
                {
                    creatureVoice.panStereo = 0.4f;
                    creatureVoice.bypassListenerEffects = false;
                    creatureVoice.bypassEffects = false;
                    creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                }
                occludeAudio.lowPassOverride = 4000f;
                audioLowPassFilter.lowpassResonanceQ = 3f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayAudioServerRpc(string smallPathAudioClip, int enumTalkativeness)
        {
            PlayAudioClientRpc(smallPathAudioClip, enumTalkativeness);
        }

        [ClientRpc]
        private void PlayAudioClientRpc(string smallPathAudioClip, int enumTalkativeness)
        {
            if (enumTalkativeness == PluginRuntimeProvider.Context.Config.Talkativeness
                || InternIdentity.Voice.CanPlayAudioAfterCooldown())
            {
                AudioManager.Instance.PlayAudio(smallPathAudioClip, InternIdentity.Voice);
            }
        }

        private void TryPlayCurrentOrderVoiceAudio(EnumVoicesState enumVoicesState)
        {
            // Default states, wait for cooldown and if no one is talking close
            this.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = enumVoicesState,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = this.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        public void TryPlayCantDoCommandVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            this.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.CantDoCommand,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = this.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        #endregion
    }
}
