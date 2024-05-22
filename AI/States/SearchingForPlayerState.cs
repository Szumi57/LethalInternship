using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches;
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
            // Check for object to grab
            if (PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch(npcController.Npc) > -1)
            {
                GameObject gameObjectGrabbleObject = ai.CheckLineOfSightForObjects();
                if (gameObjectGrabbleObject)
                {
                    GrabbableObject component = gameObjectGrabbleObject.GetComponent<GrabbableObject>();
                    if (component && !component.isHeld)
                    {
                        ai.SetDestinationToPositionInternAI(gameObjectGrabbleObject.transform.position);
                        this.targetItem = component;
                        ai.State = new FetchingObjectState(this);
                        return;
                    }
                }
            }

            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, 60, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null && ai.PlayerIsTargetable(player))
            {
                Plugin.Logger.LogDebug($"target {player.name}");
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
