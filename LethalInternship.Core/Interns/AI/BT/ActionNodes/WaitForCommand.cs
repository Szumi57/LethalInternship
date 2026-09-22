using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class WaitForCommand : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Set where the intern should look
            SetInternLookAt(ai, StartOfRound.Instance.localPlayerController.transform.position);

            // Stop
            ai.StopMoving();

            // Try play voice
            TryPlayCurrentStateVoiceAudio(ai);

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            EnumVoicesState voiceState = EnumVoicesState.Waiting;

            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = voiceState,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        private void SetInternLookAt(InternAI ai, Vector3 position)
        {
            ai.NpcController.OrderToLookAtPlayer(position + new Vector3(0, 2.35f, 0));
        }
    }
}
