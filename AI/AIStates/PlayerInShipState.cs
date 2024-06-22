using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Patches.NpcPatches;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class PlayerInShipState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.PlayerInShip;
        public override EnumAIStates GetAIState() { return STATE; }

        private Vector3 shipBoundClosestPointFromIntern
        {
            get { return InternManager.Instance.ShipBoundClosestPoint(npcController.Npc.transform.position); }
        }

        private float SqrHorizDistanceWithShipBoundPoint
        {
            get
            {
                return Vector3.Scale((shipBoundClosestPointFromIntern - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        public PlayerInShipState(AIState state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            if (ai.targetPlayer == null)
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (!ai.PlayerIsTargetable(ai.targetPlayer))
            {
                // Target is not available anymore
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (!ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            if (SqrHorizDistanceWithShipBoundPoint < Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT)
            {
                PlayerControllerBPatch.InternDropIfHoldingAnItem(ai.NpcController.Npc);
                
                // Looking
                PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (playerToLook != null)
                {
                    npcController.OrderToLookAtPlayer(playerToLook);
                }
                else
                {
                    npcController.OrderToLookForward();
                }

                // Chill
                ai.StopMoving();
                return;
            }

            ai.SetDestinationToPositionInternAI(shipBoundClosestPointFromIntern);
            if (SqrHorizDistanceWithShipBoundPoint > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            {
                npcController.OrderToSprint();
                // todo rpc
                //    SetRunningServerRpc(true);
            }
            ai.OrderMoveToDestination();
        }
    }
}
