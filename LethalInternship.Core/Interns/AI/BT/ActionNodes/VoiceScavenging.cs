using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class VoiceScavenging : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            TryPlayCurrentStateVoiceAudio(context.InternAI);

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = ai.AreHandsFree() ? EnumVoicesState.NoLoot : EnumVoicesState.HasLoot,
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
