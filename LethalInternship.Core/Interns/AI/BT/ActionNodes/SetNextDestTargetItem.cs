using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestTargetItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            
            PluginLoggerHook.LogDebug?.Invoke($"- SetNextDestTargetItem");
            context.PathController.SetNewDestination(ai.transform.position, ai.TargetItem.transform.position);
            
            return BehaviourTreeStatus.Success;
        }
    }
}
