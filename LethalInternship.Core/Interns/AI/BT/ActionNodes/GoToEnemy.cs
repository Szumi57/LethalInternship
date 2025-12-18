using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToEnemy : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("GoToEnemy Action, CurrentEnemy is null");
                return BehaviourTreeStatus.Failure;
            }

            ai.NpcController.OrderToSprint();
            ai.NpcController.OrderToLookForward();

            ai.SetDestinationToPositionInternAI(context.CurrentEnemy.transform.position);
            ai.OrderAgentAndBodyMoveToDestination();

            return BehaviourTreeStatus.Success;
        }
    }
}
