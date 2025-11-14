using Unity.Netcode;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region Emotes

        [ServerRpc(RequireOwnership = false)]
        public void StartPerformingEmoteInternServerRpc(int emoteID)
        {
            StartPerformingEmoteInternClientRpc(emoteID);
        }

        [ClientRpc]
        private void StartPerformingEmoteInternClientRpc(int emoteID)
        {
            NpcController.Npc.performingEmote = true;
            NpcController.Npc.playerBodyAnimator.SetInteger("emoteNumber", emoteID);
        }

        #endregion

        #region Stop performing emote RPC

        /// <summary>
        /// Sync the stopping the perfoming of emote between server and clients
        /// </summary>
        public void SyncStopPerformingEmote()
        {
            if (IsServer)
            {
                StopPerformingEmoteClientRpc();
            }
            else
            {
                StopPerformingEmoteServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the stopping the perfoming of emote
        /// </summary>
        [ServerRpc]
        private void StopPerformingEmoteServerRpc()
        {
            StopPerformingEmoteClientRpc();
        }

        /// <summary>
        /// Update the stopping the perfoming of emote
        /// </summary>
        [ClientRpc]
        private void StopPerformingEmoteClientRpc()
        {
            NpcController.Npc.performingEmote = false;
        }

        #endregion

        #region TooManyEmotes

        [ServerRpc(RequireOwnership = false)]
        public void PerformTooManyEmoteInternServerRpc(int tooManyEmoteID)
        {
            PerformTooManyInternClientRpc(tooManyEmoteID);
        }

        [ClientRpc]
        private void PerformTooManyInternClientRpc(int tooManyEmoteID)
        {
            NpcController.PerformTooManyEmote(tooManyEmoteID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopPerformTooManyEmoteInternServerRpc()
        {
            StopPerformTooManyInternClientRpc();
        }

        [ClientRpc]
        private void StopPerformTooManyInternClientRpc()
        {
            NpcController.StopPerformingTooManyEmote();
        }

        #endregion
    }
}
