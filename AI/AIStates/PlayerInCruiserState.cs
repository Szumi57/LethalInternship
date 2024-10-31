using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.AI.AIStates
{
    internal class PlayerInCruiserState : AIState
    {
        private VehicleController vehicleController;

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public PlayerInCruiserState(AIState state, VehicleController vehicleController) : base(state)
        {
            CurrentState = EnumAIStates.PlayerInCruiser;

            this.vehicleController = vehicleController;
        }

        /// <summary>
        /// <inheritdoc cref="AIState(InternAI)"/>
        /// </summary>
        public PlayerInCruiserState(InternAI ai, VehicleController vehicleController) : base(ai)
        {
            CurrentState = EnumAIStates.PlayerInCruiser;

            this.vehicleController = vehicleController;
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            if (vehicleController == null)
            {
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            Vector3 entryPointInternCruiser = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomEntryPosCruiser();

            if (npcController.InternAIInCruiser)
            {
                if (ai.GetVehicleCruiserTargetPlayerIsIn() == null)
                {
                    // Exit vehicle cruiser
                    ai.SyncTeleportInternVehicle(entryPointInternCruiser, enteringVehicle: false, vehicleController);
                    vehicleController.SetVehicleCollisionForPlayer(true, npcController.Npc);

                    npcController.Npc.thisController.enabled = true;
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }

                // Stay in vehicle with target player
                return;
            }

            // Intern still not in vehicle

            // No target player, search for one
            // Or Target is not available anymore
            if (ai.targetPlayer == null
                || !ai.PlayerIsTargetable(ai.targetPlayer))
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // Teleport to cruiser and enter vehicle
            npcController.Npc.thisController.enabled = false;
            vehicleController.SetVehicleCollisionForPlayer(false, npcController.Npc);

            // Place intern in random spot
            Vector3 internPassengerPos = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomInCruiserPos();
            ai.SyncTeleportInternVehicle(internPassengerPos, enteringVehicle: true, vehicleController);

            // random rotation
            float angleRandom = Random.Range(-180f, 180f);
            npcController.UpdateNowTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * npcController.Npc.thisController.transform.forward);

            // Crouch or not
            float crouchRancom = Random.Range(0f, 1f);
            if (crouchRancom > 0.5f
                && !npcController.Npc.isCrouching)
            {
                npcController.OrderToToggleCrouch();
            }

            // Chill
            npcController.OrderToStopMoving();
            return;
        }

        public override void TryPlayVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            if (!ai.InternIdentity.Voice.CanPlayAudio()
                || InternManager.Instance.DidAnInternJustTalkedClose(ai))
            {
                return;
            }

            ai.InternIdentity.Voice.PlayRandomVoiceAudio(ai.creatureVoice, EnumVoicesState.InCruiser);
            lastVoiceState = EnumVoicesState.InCruiser;
        }

        private Vector3 GetNextRandomInCruiserPos()
        {
            float x = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.x, Const.SECOND_CORNER_INTERN_IN_CRUISER.x);
            float y = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.y, Const.SECOND_CORNER_INTERN_IN_CRUISER.y);
            float z = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.z, Const.SECOND_CORNER_INTERN_IN_CRUISER.z);

            return new Vector3(x, y, z);
        }

        private Vector3 GetNextRandomEntryPosCruiser()
        {
            float x = Random.Range(Const.POS1_ENTRY_INTERN_CRUISER.x, Const.POS2_ENTRY_INTERN_CRUISER.x);

            return new Vector3(x, Const.POS1_ENTRY_INTERN_CRUISER.y, Const.POS1_ENTRY_INTERN_CRUISER.z);
        }
    }
}
