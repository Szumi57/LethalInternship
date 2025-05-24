using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextPosTargetLastKnownPosition : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            ai.NextPos = ai.TargetLastKnownPosition.Value;
            return BehaviourTreeStatus.Success;
        }
    }
}
