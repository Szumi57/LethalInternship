using LethalInternship.Core.Interns.AI;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region Voices

        public void UpdateAllInternsVoiceEffects()
        {
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.isEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.creatureVoice == null)
                {
                    continue;
                }

                internAI.UpdateInternVoiceEffects();
            }
        }

        public bool DidAnInternJustTalkedClose(int idInternTryingToTalk)
        {
            IInternAI internTryingToTalk = AllInternAIs[idInternTryingToTalk];

            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI == internTryingToTalk)
                {
                    continue;
                }

                if (internAI.InternIdentity.Voice.IsTalking()
                    && (internAI.NpcController.Npc.transform.position - internTryingToTalk.NpcController.Npc.transform.position).sqrMagnitude < VoicesConst.DISTANCE_HEAR_OTHER_INTERNS * VoicesConst.DISTANCE_HEAR_OTHER_INTERNS)
                {
                    return true;
                }
            }

            return false;
        }

        public void SyncPlayAudioIntern(int internID, string smallPathAudioClip)
        {
            AllInternAIs[internID].PlayAudioServerRpc(smallPathAudioClip, PluginRuntimeProvider.Context.Config.Talkativeness);
        }

        public void PlayAudibleNoiseForIntern(int internID,
                                              Vector3 noisePosition,
                                              float noiseRange = 10f,
                                              float noiseLoudness = 0.5f,
                                              int noiseID = 0)
        {
            IInternAI internAI = AllInternAIs[internID];
            bool noiseIsInsideClosedShip = internAI.NpcController.Npc.isInHangarShipRoom && internAI.NpcController.Npc.playersManager.hangarDoorsClosed;
            internAI.NpcController.PlayAudibleNoiseIntern(noisePosition,
                                                          noiseRange,
                                                          noiseLoudness,
                                                          timesPlayedInSameSpot: 0,
                                                          noiseIsInsideClosedShip,
                                                          noiseID);
        }

        #endregion
    }
}
