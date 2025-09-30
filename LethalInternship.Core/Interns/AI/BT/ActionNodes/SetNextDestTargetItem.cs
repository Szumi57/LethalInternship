using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestTargetItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            if (context.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("SetNextDestTargetItem TargetItem is null");
                return BehaviourTreeStatus.Failure;
            }

            context.PathController.SetNewDestination(new DJKPositionPoint(context.TargetItem.transform.position, "TargetItem"));
            
            return BehaviourTreeStatus.Success;
        }
    }
}
