using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CancelGoToItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            if (context.TargetItem == null)
            {
                if (context.nbItemsToCheck == 0)
                {
                    ai.SetCommandToFollowPlayer();
                }
                return BehaviourTreeStatus.Success;
            }

            if (!context.InternAI.IsGrabbableObjectGrabbable(context.TargetItem))
            {
                context.TargetItem = null;
                return BehaviourTreeStatus.Success;
            }

            // Voice
            TryPlayCurrentStateVoiceAudio(ai);
            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FoundLoot,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
