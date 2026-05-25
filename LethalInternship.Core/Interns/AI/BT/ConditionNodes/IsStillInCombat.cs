using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsStillInCombat : IBTCondition
    {
        private EnemyAI? lastEnemyToAttack = null;
        private float combatStartTime = -1f;
        private const float MaxCombatDuration = 10f; // seconds

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsStillInCombat Condition, CurrentEnemy is null");
                combatStartTime = -1f;
                lastEnemyToAttack = null;
                return false;
            }

            if (context.CurrentEnemy != lastEnemyToAttack)
            {
                lastEnemyToAttack = context.CurrentEnemy;
                combatStartTime = Time.time;
                return true;
            }

            if (Time.time - combatStartTime > MaxCombatDuration)
            {
                ai.SetCommandFeedback(EnumTempCommandFeedback.CombatTooLong);
                return false;
            }

            return true;
        }
    }
}
