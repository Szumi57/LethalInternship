using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class ExitNotBlocked : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.ClosestEntrance == null)
            {
                PluginLoggerHook.LogError?.Invoke("ExitNotBlocked Condition, ClosestEntrance is null !");
                return true;
            }

            return ai.ClosestEntrance.FindExitPoint();
        }
    }
}
