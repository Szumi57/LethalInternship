using GameNetcodeStuff;
using Unity.Netcode;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region AssignTargetAndSetMovingTo RPC

        /// <summary>
        /// Change the ownership of the intern to the new player target,
        /// and set the destination to him.
        /// </summary>
        /// <param name="newTarget">New <c>PlayerControllerB to set the owner of intern to.</c></param>
        public void SyncAssignTargetAndSetMovingTo(PlayerControllerB newTarget)
        {
            if (OwnerClientId != newTarget.actualClientId)
            {
                // Changes the ownership of the intern, on server and client directly
                ChangeOwnershipOfEnemy(newTarget.actualClientId);

                if (IsServer)
                {
                    SyncFromAssignTargetAndSetMovingToClientRpc(newTarget.playerClientId);
                }
                else
                {
                    SyncAssignTargetAndSetMovingToServerRpc(newTarget.playerClientId);
                }
            }
            else
            {
                AssignTargetAndSetMovingTo(newTarget.playerClientId);
            }
        }

        /// <summary>
        /// Server side, call clients to sync the set destination to new target player.
        /// </summary>
        /// <param name="playerid">Id of the new target player</param>
        [ServerRpc(RequireOwnership = false)]
        private void SyncAssignTargetAndSetMovingToServerRpc(ulong playerid)
        {
            SyncFromAssignTargetAndSetMovingToClientRpc(playerid);
        }

        /// <summary>
        /// Client side, set destination to the new target player
        /// </summary>
        /// <remarks>
        /// Change the state to <c>GetCloseToPlayerState</c>
        /// </remarks>
        /// <param name="playerid">Id of the new target player</param>
        [ClientRpc]
        private void SyncFromAssignTargetAndSetMovingToClientRpc(ulong playerid)
        {
            if (!IsOwner)
            {
                return;
            }

            AssignTargetAndSetMovingTo(playerid);
        }

        private void AssignTargetAndSetMovingTo(ulong playerid)
        {
            PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts[playerid];
            SetMovingTowardsTargetPlayer(targetPlayer);

            SetDestinationToPositionInternAI(this.targetPlayer.transform.position);

            SetCommandToFollowPlayer();
        }

        #endregion
    }
}
