using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToEnemy : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("GoToEnemy Action, CurrentEnemy is null");
                return BehaviourTreeStatus.Failure;
            }

            ai.NpcController.OrderToSprint();
            ai.NpcController.OrderToLookForward();

            ai.SetDestinationToPositionInternAI(context.CurrentEnemy.transform.position);
            ai.OrderAgentAndBodyMoveToDestination();

            // Voice
            HeldItem? weapon = ai.HeldItems.GetHeldWeaponAsHeldItem();
            if (weapon != null
                && weapon.IsWeapon)
            {
                if (weapon.IsMeleeWeapon)
                {
                    TryPlayAttackingStateVoiceAudio(ai, EnumVoicesState.AttackingWithMelee);
                }
                else if (weapon.IsRangedWeapon)
                {
                    TryPlayAttackingStateVoiceAudio(ai, EnumVoicesState.AttackingWithGun);
                }
            }

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayAttackingStateVoiceAudio(InternAI ai, EnumVoicesState enumVoicesState)
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = enumVoicesState,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
