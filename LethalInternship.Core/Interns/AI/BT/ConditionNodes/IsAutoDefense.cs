using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsAutoDefense : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsAutoDefense Condition, CurrentEnemy is null");
                return false;
            }

            if (!ai.InternIdentity.AutoDefense)
            {
                ai.SetCommandFeedback(EnumTempCommandFeedback.NotInAutoDefense);
                return false;
            }

            return true;
        }
    }
}
