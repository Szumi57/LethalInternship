using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
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
            // Why cancel ?
            // Target item null ?
            if (context.TargetItem == null)
            {
                if (context.InternAI.CurrentCommand == EnumCommandTypes.GoFetchItem)
                {
                    // Item grabbed and/or null
                    ai.SetCommandToFollowPlayer(playVoice: false);
                }
                else if (context.nbItemsToCheck == 0) // while scavenging
                {
                    ai.SetCommandFeedback(EnumTempCommandFeedback.NoItemsLeftToGrab);
                    if (ai.AreHandsFree())
                    {
                        ai.SetCommandToFollowPlayer(playVoice: false);
                    }
                    else
                    {
                        // else return scavenged items to ship
                        PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} context.TargetItem == null, context.nbItemsToCheck == 0, !ai.AreHandsFree()");
                        return BehaviourTreeStatus.Failure;
                    }
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
                    ai.SetCommandFeedback(EnumTempCommandFeedback.Thinking);
                }
                return BehaviourTreeStatus.Success;
            }

            // Or can't hold or not grabbable
            bool canHoldItem = context.InternAI.CanHoldItem(context.TargetItem);
            bool isGrabbableObjectGrabbable = InternManager.Instance.IsGrabbableObjectGrabbable(context.TargetItem);
            if (!canHoldItem
                || !isGrabbableObjectGrabbable)
            {
                if (!canHoldItem)
                    ai.SetCommandFeedback(EnumTempCommandFeedback.CantHoldItem);
                if (!isGrabbableObjectGrabbable)
                    ai.SetCommandFeedback(EnumTempCommandFeedback.TargetItemNotGrabbable);

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
