using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class ChillFrontOfEntrance : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Chill
            ai.StopMoving();

            return BehaviourTreeStatus.Success;
        }
    }
}
