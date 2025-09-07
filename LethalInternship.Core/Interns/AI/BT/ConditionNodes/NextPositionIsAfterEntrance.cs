using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class NextPositionIsAfterEntrance : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;
            Vector3 nextPoint = ai.PointOfInterest?.GetPoint() ?? ai.NextPos;

            if (ai.isOutside && nextPoint.y < -80f)
            {
                PluginLoggerHook.LogDebug?.Invoke($"NextPositionIsAfterEntrance");
                return true;
            }

            if (!ai.isOutside && nextPoint.y >= -80f)
            {
            PluginLoggerHook.LogDebug?.Invoke($"NextPositionIsAfterEntrance");
                return true;
            }

            return false;
        }
    }
}
