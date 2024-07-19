using LethalInternship.Enums;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.AI.AIStates
{
    internal class PlayerInCruiserState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.PlayerInCruiser;
        /// <summary>
        /// <inheritdoc cref="AIState.GetAIState"/>
        /// </summary>
        public override EnumAIStates GetAIState() { return STATE; }

        private VehicleController vehicleController;
        private bool isInternInCruiser;

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public PlayerInCruiserState(AIState state, VehicleController vehicleController) : base(state)
        {
            this.vehicleController = vehicleController;
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            Vector3 entryPointInternCruiser = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomEntryPosCruiser();
            //RayUtil.RayCastAndDrawFromPointWithColor(ai.LineRendererUtil.GetLineRenderer(), npcController.Npc.transform.position, entryPointInternCruiser, Color.white);
            //RayUtil.RayCastAndDrawFromPointWithColor(ai.LineRendererUtil.GetLineRenderer(), npcController.Npc.transform.position, ai.destination, Color.magenta);

            if (isInternInCruiser)
            {
                if (!ai.IsTargetPlayerInCruiserVehicle())
                {
                    // Exit vehicle cruiser
                    Plugin.LogDebug($"intern #{ai.NpcController.Npc.playerClientId} exits vehicle");

                    ai.TeleportInternAndSync(entryPointInternCruiser, !ai.isOutside, isUsingEntrance: false);
                    vehicleController.SetVehicleCollisionForPlayer(true, npcController.Npc);

                    ai.ReParentIntern(npcController.Npc.playersManager.playersContainer);

                    isInternInCruiser = false;
                    npcController.SetAiInCruiser(isInternInCruiser);
                    npcController.Npc.thisController.enabled = true;
                    ai.State = new GetCloseToPlayerState(this);
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

            // Enter vehicle cruiser
            if ((ai.destination - npcController.Npc.transform.position).sqrMagnitude < npcController.Npc.grabDistance * npcController.Npc.grabDistance)
            {
                npcController.Npc.thisController.enabled = false;
                vehicleController.SetVehicleCollisionForPlayer(false, npcController.Npc);

                Vector3 internPassengerPos = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomInCruiserPos();
                ai.TeleportInternAndSync(internPassengerPos, !ai.isOutside, isUsingEntrance: false);
                ai.ReParentIntern(vehicleController.transform);

                // Chill
                isInternInCruiser = true;
                npcController.SetAiInCruiser(isInternInCruiser);
                npcController.OrderToStopMoving();
                return;
            }

            // Go to the cruiser
            ai.SetDestinationToPositionInternAI(entryPointInternCruiser);
            npcController.OrderToSprint();
            ai.OrderMoveToDestination();
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
