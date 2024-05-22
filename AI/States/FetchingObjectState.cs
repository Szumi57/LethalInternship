using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace LethalInternship.AI.States
{
    internal class FetchingObjectState : State
    {
        private static readonly EnumStates STATE = EnumStates.FetchingObject;
        public override EnumStates GetState() { return STATE; }

        public FetchingObjectState(State state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            if (this.targetItem == null)
            {
                ai.State = new JustLostPlayerState(this);
                return;
            }

            npcController.OrderToLookAtPosition(this.targetItem.transform.position);

            if ((ai.destination - npcController.Npc.transform.position).sqrMagnitude < npcController.Npc.grabDistance * npcController.Npc.grabDistance)
            {
                if (!npcController.Npc.inAnimationWithEnemy && !npcController.Npc.activatingItem)
                {
                    PlayerControllerBPatch.BeginGrabObject_ReversePatch(npcController.Npc, this.targetItem);
                    this.targetItem = null;
                    ai.State = new JustLostPlayerState(this);
                }
            }

            ai.OrderMoveToDestination();
        }
    }
}
