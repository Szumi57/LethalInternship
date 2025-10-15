using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CancelGoToItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            if (context.TargetItem == null)
            {
                if (context.nbItemsToCheck == 0)
                {
                    ai.SetCommandToFollowPlayer();
                }
                // nbItemsToCheck > 0, still calculating paths to items
                return BehaviourTreeStatus.Success;
            }

            if (!context.InternAI.IsGrabbableObjectGrabbable(context.TargetItem))
            {
                context.TargetItem = null;
                return BehaviourTreeStatus.Success;
            }

            // Voice
            return BehaviourTreeStatus.Success;
        }
    }
}
