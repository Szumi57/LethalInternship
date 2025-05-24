using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextPosTargetItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            ai.NextPos = ai.TargetItem.transform.position;
            return BehaviourTreeStatus.Success;
        }
    }
}
