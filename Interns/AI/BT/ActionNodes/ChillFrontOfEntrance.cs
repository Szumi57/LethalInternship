using LethalInternship.BehaviorTree;

namespace LethalInternship.Interns.AI.BT.ActionNodes
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
