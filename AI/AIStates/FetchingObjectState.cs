using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;

namespace LethalInternship.AI.AIStates
{
    internal class FetchingObjectState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.FetchingObject;
        public override EnumAIStates GetAIState() { return STATE; }

        public FetchingObjectState(AIState state, GrabbableObject targetItem) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }

            this.targetItem = targetItem;
        }

        public override void DoAI()
        {
            if (this.targetItem == null)
            {
                ai.State = new JustLostPlayerState(this);
                return;
            }

            if (!ai.IsGrabbableObjectGrabbable(this.targetItem))
            {
                this.targetItem = null;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            ai.SetDestinationToPositionInternAI(this.targetItem.transform.position);
            npcController.OrderToLookAtPosition(this.targetItem.transform.position);

            Plugin.Logger.LogDebug($"{ai.NpcController.Npc.playerUsername} try to grab {this.targetItem.name}");
            if ((ai.destination - npcController.Npc.transform.position).sqrMagnitude < npcController.Npc.grabDistance * npcController.Npc.grabDistance)
            {
                if (!npcController.Npc.inAnimationWithEnemy && !npcController.Npc.activatingItem)
                {
                    PlayerControllerBPatch.BeginGrabObject_ReversePatch(npcController.Npc, this.targetItem);
                    if (ai.HandsFree())
                    {
                        // Problem with taking object
                        ai.ListInvalidObjects.Add(this.targetItem);
                    }

                    this.targetItem = null;
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }
            }

            ai.OrderMoveToDestination();
        }
    }
}
