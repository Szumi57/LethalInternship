using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region UpdatePlayerPosition RPC

        /// <summary>
        /// Sync the intern position between server and clients.
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="inShipRoom">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        public void SyncUpdateInternPosition(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            if (IsServer)
            {
                UpdateInternPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
            else
            {
                UpdateInternPositionServerRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
        }

        /// <summary>
        /// Server side, call clients to sync the new position of the intern
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="inShipRoom">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        [ServerRpc(RequireOwnership = false)]
        private void UpdateInternPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            UpdateInternPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
        }

        /// <summary>
        /// Update the intern position if not owner of intern, the owner move on his side the intern.
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="isInShip">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        [ClientRpc]
        private void UpdateInternPositionClientRpc(Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
        {
            if (NpcController == null)
            {
                return;
            }

            bool flag = NpcController.Npc.currentFootstepSurfaceIndex == 8 && (IsOwner && NpcController.IsTouchingGround || isPlayerGrounded);
            if (NpcController.Npc.bleedingHeavily || flag)
            {
                NpcController.Npc.DropBlood(Vector3.down, NpcController.Npc.bleedingHeavily, flag);
            }
            NpcController.Npc.timeSincePlayerMoving = 0f;

            if (IsOwner)
            {
                // Only update if not owner
                return;
            }

            NpcController.Npc.isExhausted = exhausted;
            NpcController.Npc.isInElevator = inElevator;
            NpcController.Npc.isInHangarShipRoom = isInShip;

            foreach (var item in HeldItems.Items)
            {
                if (item.GrabbableObject == null)
                {
                    continue;
                }

                if (item.GrabbableObject.isInShipRoom != isInShip)
                {
                    item.GrabbableObject.isInElevator = inElevator;
                    NpcController.Npc.SetItemInElevator(droppedInShipRoom: isInShip, droppedInElevator: inElevator, item.GrabbableObject);
                }
            }

            NpcController.Npc.oldPlayerPosition = NpcController.Npc.serverPlayerPosition;
            if (!NpcController.Npc.inVehicleAnimation)
            {
                NpcController.Npc.serverPlayerPosition = newPos;
            }
        }

        #endregion

        #region UpdatePlayerRotation and look RPC

        /// <summary>
        /// Sync the intern body rotation and rotation of head (where he looks) between server and clients.
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        public void SyncUpdateInternRotationAndLook(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (IsServer)
            {
                UpdateInternRotationAndLookClientRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
            else
            {
                UpdateInternRotationAndLookServerRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
        }

        /// <summary>
        /// Server side, call clients to update intern body rotation and rotation of head (where he looks)
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        [ServerRpc(RequireOwnership = false)]
        private void UpdateInternRotationAndLookServerRpc(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            UpdateInternRotationAndLookClientRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
        }

        /// <summary>
        /// Client side, update the intern body rotation and rotation of head (where he looks).
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        [ClientRpc]
        private void UpdateInternRotationAndLookClientRpc(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (NpcController == null)
            {
                return;
            }

            if (IsClientOwnerOfIntern())
            {
                // Only update if not owner
                return;
            }

            // Update state indicator
            // Actually, too much cluter, indicator just for owner for now
            //this.stateIndicatorServer = stateIndicator;

            // Update direction
            NpcController.SetTurnBodyTowardsDirection(direction);
            switch ((EnumObjectsLookingAt)intEnumObjectsLookingAt)
            {
                case EnumObjectsLookingAt.Forward:
                    NpcController.OrderToLookForward();
                    break;
                case EnumObjectsLookingAt.Player:
                    NpcController.OrderToLookAtPlayer(playerEyeToLookAt);
                    break;
                case EnumObjectsLookingAt.Position:
                    NpcController.OrderToLookAtPosition(positionToLookAt);
                    break;
            }
        }

        #endregion

        #region UpdatePlayer animations RPC

        /// <summary>
        /// Server side, call client to sync changes in animation of the intern
        /// </summary>
        /// <param name="animationState">Current animation state</param>
        /// <param name="animationSpeed">Current animation speed</param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateInternAnimationServerRpc(int animationState, float animationSpeed)
        {
            UpdateInternAnimationClientRpc(animationState, animationSpeed);
        }

        /// <summary>
        /// Client, update changes in animation of the intern
        /// </summary>
        /// <param name="animationState">Current animation state</param>
        /// <param name="animationSpeed">Current animation speed</param>
        [ClientRpc]
        private void UpdateInternAnimationClientRpc(int animationState, float animationSpeed)
        {
            if (NpcController == null)
            {
                return;
            }

            if (IsClientOwnerOfIntern())
            {
                // Only update if not owner
                return;
            }

            NpcController.ApplyUpdateInternAnimationsNotOwner(animationState, animationSpeed);
        }

        #endregion

        #region UpdateSpecialAnimation RPC

        /// <summary>
        /// Sync the changes in special animation of the intern body, between server and clients
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        public void UpdateInternSpecialAnimationValue(bool specialAnimation, float timed, bool climbingLadder)
        {
            if (!IsClientOwnerOfIntern())
            {
                return;
            }

            UpdateInternSpecialAnimationServerRpc(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Server side, call clients to update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        [ServerRpc(RequireOwnership = false)]
        private void UpdateInternSpecialAnimationServerRpc(bool specialAnimation, float timed, bool climbingLadder)
        {
            UpdateInternSpecialAnimationClientRpc(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Client side, update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        [ClientRpc]
        private void UpdateInternSpecialAnimationClientRpc(bool specialAnimation, float timed, bool climbingLadder)
        {
            UpdateInternSpecialAnimation(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        private void UpdateInternSpecialAnimation(bool specialAnimation, float timed, bool climbingLadder)
        {
            if (NpcController == null)
            {
                return;
            }

            PlayerControllerBHook.IsInSpecialAnimationClientRpc_ReversePatch?.Invoke(NpcController.Npc, specialAnimation, timed, climbingLadder);
            NpcController.Npc.ResetZAndXRotation();
        }

        #endregion


        #region SyncDeadBodyPosition RPC

        /// <summary>
        /// Server side, call the clients to update the dead body of the intern
        /// </summary>
        /// <param name="newBodyPosition">New dead body position</param>
        [ServerRpc(RequireOwnership = false)]
        public void SyncDeadBodyPositionServerRpc(Vector3 newBodyPosition)
        {
            SyncDeadBodyPositionClientRpc(newBodyPosition);
        }

        /// <summary>
        /// Client side, update the dead body of the intern
        /// </summary>
        /// <param name="newBodyPosition">New dead body position</param>
        [ClientRpc]
        private void SyncDeadBodyPositionClientRpc(Vector3 newBodyPosition)
        {
            PlayerControllerBHook.SyncBodyPositionClientRpc_ReversePatch?.Invoke(NpcController.Npc, newBodyPosition);
        }

        #endregion

        #region SyncFaceUnderwater

        [ServerRpc(RequireOwnership = false)]
        public void SyncSetFaceUnderwaterServerRpc(bool isUnderwater)
        {
            SyncSetFaceUnderwaterClientRpc(isUnderwater);
        }

        [ClientRpc]
        private void SyncSetFaceUnderwaterClientRpc(bool isUnderwater)
        {
            NpcController.Npc.isUnderwater = isUnderwater;
        }

        #endregion



        #region Jump RPC

        /// <summary>
        /// Sync the intern doing a jump between server and clients
        /// </summary>
        public void SyncJump()
        {
            if (IsServer)
            {
                JumpClientRpc();
            }
            else
            {
                JumpServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the intern doing a jump
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void JumpServerRpc()
        {
            JumpClientRpc();
        }

        /// <summary>
        /// Client side, update the action of intern doing a jump
        /// only for not the owner
        /// </summary>
        [ClientRpc]
        private void JumpClientRpc()
        {
            if (!IsClientOwnerOfIntern())
            {
                PlayerControllerBHook.PlayJumpAudio_ReversePatch?.Invoke(NpcController.Npc);
            }
        }

        #endregion

        #region Land from Jump RPC

        /// <summary>
        /// Sync the landing of the jump of the intern, between server and clients
        /// </summary>
        /// <param name="fallHard"></param>
        public void SyncLandFromJump(bool fallHard)
        {
            if (IsServer)
            {
                JumpLandFromClientRpc(fallHard);
            }
            else
            {
                JumpLandFromServerRpc(fallHard);
            }
        }

        /// <summary>
        /// Server side, call clients to update the action of intern land from jump
        /// </summary>
        /// <param name="fallHard"></param>
        [ServerRpc(RequireOwnership = false)]
        private void JumpLandFromServerRpc(bool fallHard)
        {
            JumpLandFromClientRpc(fallHard);
        }

        /// <summary>
        /// Client side, update the action of intern land from jump
        /// </summary>
        /// <param name="fallHard"></param>
        [ClientRpc]
        private void JumpLandFromClientRpc(bool fallHard)
        {
            if (fallHard)
            {
                NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
                return;
            }
            NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 0.7f);
        }

        #endregion

        #region Sinking RPC

        /// <summary>
        /// Sync the state of sink of the intern between server and clients
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        public void SyncChangeSinkingState(bool startSinking, float sinkingSpeed = 0f, int audioClipIndex = 0)
        {
            if (IsServer)
            {
                ChangeSinkingStateClientRpc(startSinking, sinkingSpeed, audioClipIndex);
            }
            else
            {
                ChangeSinkingStateServerRpc(startSinking, sinkingSpeed, audioClipIndex);
            }
        }

        /// <summary>
        /// Server side, call clients to update the state of sink of the intern
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        [ServerRpc]
        private void ChangeSinkingStateServerRpc(bool startSinking, float sinkingSpeed, int audioClipIndex)
        {
            ChangeSinkingStateClientRpc(startSinking, sinkingSpeed, audioClipIndex);
        }

        /// <summary>
        /// Client side, update the state of sink of the intern
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        [ClientRpc]
        private void ChangeSinkingStateClientRpc(bool startSinking, float sinkingSpeed, int audioClipIndex)
        {
            if (startSinking)
            {
                NpcController.Npc.sinkingSpeedMultiplier = sinkingSpeed;
                NpcController.Npc.isSinking = true;
                NpcController.Npc.statusEffectAudio.clip = StartOfRound.Instance.statusEffectClips[audioClipIndex];
                NpcController.Npc.statusEffectAudio.Play();
            }
            else
            {
                StopSinkingState();
            }
        }

        public void StopSinkingState()
        {
            NpcController.Npc.isSinking = false;
            NpcController.Npc.statusEffectAudio.volume = 0f;
            NpcController.Npc.statusEffectAudio.Stop();
            NpcController.Npc.voiceMuffledByEnemy = false;
            NpcController.Npc.sourcesCausingSinking = 0;
            NpcController.Npc.isMovementHindered = 0;
            NpcController.Npc.hinderedMultiplier = 1f;

            NpcController.Npc.isUnderwater = false;
            NpcController.Npc.underwaterCollider = null;
        }

        #endregion

        #region Disable Jetpack RPC

        /// <summary>
        /// Sync the disabling of jetpack mode between server and clients
        /// </summary>
        public void SyncDisableJetpackMode()
        {
            if (IsServer)
            {
                DisableJetpackModeClientRpc();
            }
            else
            {
                DisableJetpackModeServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the disabling of jetpack mode between server and clients
        /// </summary>
        [ServerRpc]
        private void DisableJetpackModeServerRpc()
        {
            DisableJetpackModeClientRpc();
        }

        /// <summary>
        /// Client side, update the disabling of jetpack mode between server and clients
        /// </summary>
        [ClientRpc]
        private void DisableJetpackModeClientRpc()
        {
            NpcController.Npc.DisableJetpackControlsLocally();
        }

        #endregion

        #region BillBoard

        public Vector3 GetBillBoardPosition(GameObject bodyModel)
        {
            return npcController.GetBillBoardPosition(bodyModel, Npc.usernameCanvas.transform.localPosition);
        }

        #endregion
    }
}
