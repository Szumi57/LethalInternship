using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToPosition : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.CanRun)
            {
                float sqrHorizontalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
                float sqrVerticalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;

                if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                     || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
                {
                    ai.NpcController.OrderToSprint();
                }
                else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
                {
                    ai.NpcController.OrderToStopSprint();
                }
            }

            ai.NpcController.OrderToLookForward();

            ai.SetDestinationToPositionInternAI(ai.NextPos);
            ai.OrderAgentAndBodyMoveToDestination();

            if (ai.CurrentCommand == EnumCommandTypes.FollowPlayer)
            {
                ai.FollowCrouchIfCanDo();
                TryPlayCurrentStateVoiceAudio(ai);
            }

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Priority state
            // Stop talking and voice new state
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FollowingPlayer,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
