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
            get { return InternManager.ShipBoundClosestPoint(npcController.Npc.transform.position); }
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
                // Chill
                ai.StopMoving();
                return;
            }

            ai.SetDestinationToPositionInternAI(shipBoundClosestPointFromIntern);
            ai.OrderMoveToDestination();
        }
    }
}
