using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsTargetInVehicle
    {
        public bool Condition(InternAI ai)
        {
            if (ai.targetPlayer == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsTargetInVehicle condition, targetPlayer is null !");
                return false;
            }

            return ai.targetPlayer.inVehicleAnimation;
        }
    }
}
