using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestTargetLastKnownPosition : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            //if (context.TargetLastKnownPosition == null)
            //{
            //    PluginLoggerHook.LogError?.Invoke("SetNextDestTargetLastKnownPosition action, TargetLastKnownPosition is null");
            //    return BehaviourTreeStatus.Failure;
            //}

            context.PathController.SetNewDestination(new DJKMovingPoint(context.InternAI.targetPlayer.transform, $"targetPlayer {context.InternAI.targetPlayer.playerUsername}"));
            
            return BehaviourTreeStatus.Success;
        }
    }
}
