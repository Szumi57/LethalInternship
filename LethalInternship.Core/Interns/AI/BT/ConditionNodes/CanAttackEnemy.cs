using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class CanAttackEnemy : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("CanAttackEnemy Condition, CurrentEnemy is null");
                return false;
            }

            if (context.CurrentEnemy.isEnemyDead
                || ai.isOutside != context.CurrentEnemy.isOutside)
            {
                return false;
            }

            HeldItem? weapon = ai.HeldItems.GetHeldWeaponAsHeldItem();
            if (weapon == null
                || !weapon.IsWeapon)
            {
                return false;
            }

            if (!InternManager.Instance.IsEnemyKillable(context.CurrentEnemy))
            {
                return false;
            }

            return true;
        }
    }
}
