using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI.Commands
{
    public class FetchingObjectCommand : ICommandAI
    {
        private readonly InternAI ai;

        private NpcController Controller { get { return ai.NpcController; } }
        private GrabbableObject? TargetItem { get { return ai.TargetItem; } set { ai.TargetItem = value; } }

        public FetchingObjectCommand(InternAI internAI, GrabbableObject targetItem)
        {
            ai = internAI;
            ai.TargetItem = targetItem;
        }

        public void Execute()
        {
            // Target item invalid to grab
            if (ai.HeldItem != null
                || TargetItem == null
                || !ai.IsGrabbableObjectGrabbable(TargetItem))
            {
                TargetItem = null;
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return;
            }

            float sqrMagDistanceItem = (TargetItem.transform.position - Controller.Npc.transform.position).sqrMagnitude;
            // Close enough to item for grabbing, attempt to grab
            if (sqrMagDistanceItem < Controller.Npc.grabDistance * Controller.Npc.grabDistance * Plugin.Config.InternSizeScale.Value)
            {
                if (!Controller.Npc.inAnimationWithEnemy
                    && !Controller.Npc.activatingItem)
                {
                    ai.GrabItemServerRpc(TargetItem.NetworkObject, itemGiven: false);
                    TargetItem = null;
                    ai.QueueNewCommand(new FollowPlayerCommand(ai));
                    return;
                }
            }

            // Else get close to item
            ai.SetDestinationToPositionInternAI(TargetItem.transform.position);

            // Look at item or not if hidden by stuff
            if (!Physics.Linecast(Controller.Npc.gameplayCamera.transform.position, TargetItem.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                Controller.OrderToLookAtPosition(TargetItem.transform.position);
            }
            else
            {
                Controller.OrderToLookForward();
            }

            // Sprint if far enough from the item
            if (sqrMagDistanceItem > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            {
                Controller.OrderToSprint();
            }

            ai.OrderAgentAndBodyMoveToDestination();

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return;
        }

        public void PlayerHeard(Vector3 noisePosition) { }

        private void TryPlayCurrentStateVoiceAudio()
        {
            // Talk if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FoundLoot,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public string GetBillboardStateIndicator()
        {
            return "!!";
        }

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.None;
        }
    }
}
