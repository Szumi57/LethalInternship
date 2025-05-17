using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class ExitNotBlocked
    {
        public bool Condition(InternAI ai)
        {
            if (ai.ClosestEntrance == null)
            {
                PluginLoggerHook.LogError?.Invoke("ExitNotBlocked Condition, ClosestEntrance is null !");
                return true;
            }

            return ai.ClosestEntrance.FindExitPoint();
        }
    }
}
