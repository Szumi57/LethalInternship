using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CancelGoAttack : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null
                || context.CurrentEnemy.isEnemyDead
                || ai.isOutside != context.CurrentEnemy.isOutside)
            {
                ai.SetCommandFeedback(EnumTempCommandFeedback.EnemyNotHere);
                context.InternAI.SetCommandToFollowPlayer(playVoice: false);
                return BehaviourTreeStatus.Success;
            }

            HeldItem? weapon = ai.HeldItems.GetHeldWeaponAsHeldItem();
            if (weapon == null
                || !weapon.IsWeapon)
            {
                ai.SetCommandFeedback(EnumTempCommandFeedback.NotHoldingWeapon);
            }

            if (!InternManager.Instance.IsEnemyKillable(context.CurrentEnemy))
            {
                ai.SetCommandFeedback(EnumTempCommandFeedback.CantKillEnemy);
            }

            context.InternAI.SetCommandToFollowPlayer(playVoice: false);
            return BehaviourTreeStatus.Success;
        }
    }
}
