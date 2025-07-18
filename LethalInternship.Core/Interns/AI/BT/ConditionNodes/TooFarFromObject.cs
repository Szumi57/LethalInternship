using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromObject : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("targetItem is null");
                return false;
            }

            float sqrMagDistanceItem = (ai.TargetItem.transform.position - ai.NpcController.Npc.transform.position).sqrMagnitude;
            // Close enough to item for grabbing
            if (sqrMagDistanceItem < ai.NpcController.Npc.grabDistance * ai.NpcController.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale)
            {
                return false;
            }

            return true;
        }
    }
}
