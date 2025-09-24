using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestTargetItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            if (context.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("TargetItem is null");
                return BehaviourTreeStatus.Failure;
            }

            context.PathController.SetNewDestination(context.TargetItem.transform.position);
            
            return BehaviourTreeStatus.Success;
        }
    }
}
