using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsObjectToGrab : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Check for object to grab
            if (!ai.AreHandsFree())
            {
                return false;
            }

            GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
            if (grabbableObject == null)
            {
                return false;
            }

            // Voice
            TryPlayCurrentStateVoiceAudio(ai);

            context.TargetItem = grabbableObject;
            return true;
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
