using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class PlayerInShipState : State
    {
        private static readonly EnumStates STATE = EnumStates.PlayerInShip;
        public override EnumStates GetState() { return STATE; }

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

        public PlayerInShipState(State state) : base(state)
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
