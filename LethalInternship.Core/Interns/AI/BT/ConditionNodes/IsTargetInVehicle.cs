using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsTargetInVehicle : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.targetPlayer == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsTargetInVehicle condition, targetPlayer is null !");
                return false;
            }

            return ai.targetPlayer.inVehicleAnimation;
        }
    }
}
