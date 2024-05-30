using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;
using System.ComponentModel;
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
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    ai.SetDestinationToPositionInternAI(grabbableObject.transform.position);
                    this.targetItem = grabbableObject;
                    ai.State = new FetchingObjectState(this);
                    return;
                }
            }

            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, 60, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                Plugin.Logger.LogDebug($"target {player.name}");
                // new target
                ai.AssignTargetAndSetMovingTo(player);
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
