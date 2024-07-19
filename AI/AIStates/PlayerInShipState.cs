using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
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

        /// <summary>
        /// <inheritdoc cref="InternManager.ShipBoundClosestPoint"/>
        /// </summary>
        private Vector3 shipBoundClosestPointFromIntern
        {
            get { return InternManager.Instance.ShipBoundClosestPoint(npcController.Npc.transform.position); }
        }

        /// <summary>
        /// Represents the distance between the body of intern (<c>PlayerControllerB</c> position) and the closest point of the ship, 
        /// only on axis x and z, y at 0, and squared
        /// </summary>
        private float SqrHorizDistanceWithShipBoundPoint
        {
            get
            {
                return Vector3.Scale((shipBoundClosestPointFromIntern - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

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
            if (!ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            // The target player is in the ship or too close to it
            // and the intern is close enough of the closest point of the ship
            // The intern stop moving and drop his item
            if (SqrHorizDistanceWithShipBoundPoint < Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT)
            {
                if (!ai.AreHandsFree())
                {
                    // Intern drop item
                    ai.DropItemServerRpc();
                }

                // Looking
                PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
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

            // Target player in ship or too close to it
            // the intern still need to get close to the ship
            ai.SetDestinationToPositionInternAI(shipBoundClosestPointFromIntern);
            if (SqrHorizDistanceWithShipBoundPoint > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            {
                npcController.OrderToSprint();
            }
            ai.OrderMoveToDestination();
        }
    }
}
