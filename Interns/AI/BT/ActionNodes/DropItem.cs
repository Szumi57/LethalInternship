using LethalInternship.BehaviorTree;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class DropItem
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            if (ai.AreHandsFree())
            {
                Plugin.LogError("DropItem action failed, no item held !");
                return BehaviourTreeStatus.Failure;
            }

            ai.DropItem();

            return BehaviourTreeStatus.Success;
        }
    }
}
