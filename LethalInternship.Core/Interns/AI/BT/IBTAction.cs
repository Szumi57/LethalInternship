using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT
{
    public interface IBTAction
    {
        BehaviourTreeStatus Action(BTContext context);
    }
}
