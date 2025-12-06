using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
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
                    if (ai.AreHandsFree())
                    {
                        ai.SetCommandToFollowPlayer(playVoice: false);
                    }
                    // else return scavenged items to ship
                }
                else
                {
                    // nbItemsToCheck > 0, still calculating paths to items
                    PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} THINKING");

                    ai.StopMoving();
                    ai.NpcController.OrderToLookForward();
                    if (ai.NpcController.Npc.isCrouching)
                    {
                        ai.NpcController.OrderToToggleCrouch();
                    }
                    TryPlayThinkingVoiceAudio(ai);
                }
                return BehaviourTreeStatus.Success;
            }

            if (!context.InternAI.IsGrabbableObjectGrabbable(context.TargetItem))
            {
                context.TargetItem = null;
                ai.TryPlayCantDoCommandVoiceAudio();
                return BehaviourTreeStatus.Success;
            }

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayThinkingVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.Thinking,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
