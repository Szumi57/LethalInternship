using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class ChillFrontOfEntrance
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            // Chill
            ai.StopMoving();

            return BehaviourTreeStatus.Success;
        }
    }
}
