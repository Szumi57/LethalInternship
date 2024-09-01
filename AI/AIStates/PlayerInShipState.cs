using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Utils;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the target player (owner player) is in ship or close to it,
    /// the intern drop his item and wait outside
    /// </summary>
    internal class PlayerInShipState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.PlayerInShip;
        /// <summary>
        /// <inheritdoc cref="AIState.GetAIState"/>
        /// </summary>
        public override EnumAIStates GetAIState() { return STATE; }

        private Vector3? ShipBoundClosestPointFromIntern = null;

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public PlayerInShipState(AIState state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // No target player, search for one
            // Or Target is not available anymore
            if (ai.targetPlayer == null
                || !ai.PlayerIsTargetable(ai.targetPlayer))
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            // If target player not in the ship or too close to it, the intern follow him
            if (!ai.IsPlayerInShipBoundsExpanded(ai.targetPlayer))
            {
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            // The target player is in the ship or too close to it
            // and the intern is close enough of the closest point of the ship or in the ship
            // The intern stop moving and drop his item
            //float sqrHorizDistanceWithDestination = Vector3.Scale((ai.destination - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            //if (sqrHorizDistanceWithDestination < Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT)
            if (InternManager.Instance.GetExpandedShipBounds().Contains(npcController.Npc.transform.position))
            {
                if (!ai.AreHandsFree())
                {
                    // Intern drop item
                    ai.DropItemServerRpc();
                }

                // Looking
                PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(180, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (playerToLook != null)
                {
                    npcController.OrderToLookAtPlayer(playerToLook.playerEye.position);
                }
                else
                {
                    npcController.OrderToLookForward();
                }

                // Chill
                ai.StopMoving();
                return;
            }

            ai.SetDestinationToPositionInternAI(ai.targetPlayer.transform.position);
            ai.OrderMoveToDestination();

            //// Target player in ship or too close to it
            //// the intern still need to get close to the ship
            //if (!ShipBoundClosestPointFromIntern.HasValue)
            //{
            //    ShipBoundClosestPointFromIntern = InternManager.Instance.ShipBoundClosestPoint(npcController.Npc.transform.position);
            //    Plugin.LogDebug($"first ShipBoundClosestPointFromIntern {ShipBoundClosestPointFromIntern}, ");
            //}

            //ai.SetDestinationToPositionInternAI(ShipBoundClosestPointFromIntern.Value);
            //if (sqrHorizDistanceWithDestination > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            //{
            //    npcController.OrderToSprint();
            //}
            //Plugin.LogDebug($"ShipBoundClosestPointFromIntern {ShipBoundClosestPointFromIntern}, npcController.Npc.transform.position {npcController.Npc.transform.position}");
            //ai.OrderMoveToDestination();
            //// Destination after path checking might be not the same now
            //ShipBoundClosestPointFromIntern = ai.destination;
        }

        public override string GetBillboardStateIndicator()
        {
            return "...";
        }
    }
}
