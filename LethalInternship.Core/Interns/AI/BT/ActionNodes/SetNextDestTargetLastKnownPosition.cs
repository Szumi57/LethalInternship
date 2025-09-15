using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestTargetLastKnownPosition : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            context.PathController.SetNewDestination(ai.transform.position, ai.TargetLastKnownPosition.Value);
            
            return BehaviourTreeStatus.Success;
        }
    }
}
