using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GrabItemBehavior : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.NpcController.Npc.inAnimationWithEnemy
                    || ai.NpcController.Npc.activatingItem)
            {
                return BehaviourTreeStatus.Running;
            }

            ai.GrabItemServerRpc(ai.TargetItem.NetworkObject, itemGiven: false);
            ai.TargetItem = null;
            return BehaviourTreeStatus.Success;
        }
    }
}
