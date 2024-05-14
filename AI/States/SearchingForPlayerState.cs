using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class SearchingForPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.SearchingForPlayer;
        public override EnumStates GetState() { return STATE; }

        private PlayerControllerB? player;

        public SearchingForPlayerState(State newState) : base(newState) { }
        public SearchingForPlayerState(InternAI ai) : base(ai) { }

        public override void DoAI()
        {
            player = ai.CheckLineOfSightForClosestPlayer(Const.INTERN_FOV, 60, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null && ai.PlayerIsTargetable(player))
            {
                // new target
                ai.SetMovingTowardsTargetPlayer(player);
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            ai.OrderMoveToDestination();

            if (!searchForPlayers.inProgress)
            {
                ai.StartSearch(ai.transform.position, searchForPlayers);
            }
        }
    }
}
