using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsStillInCombat : IBTCondition
    {
        private EnemyAI lastEnemyToAttack = null!;
        private long combatTimer = 10000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeTick;
        private long combatDuration;

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsStillInCombat Condition, CurrentEnemy is null");
                return false;
            }

            // Combat duration ?
            if (lastEnemyToAttack == context.CurrentEnemy)
            {
                combatDuration += DateTime.Now.Ticks - lastTimeTick;
                if (combatDuration > combatTimer)
                {
                    ai.SetCommandFeedback(EnumTempCommandFeedback.CombatTooLong);
                    return false;
                }
            }
            else
            {
                lastEnemyToAttack = context.CurrentEnemy;
                combatDuration = 0;
            }
            lastTimeTick = DateTime.Now.Ticks;

            return true;
        }
    }
}
