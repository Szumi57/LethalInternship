using LethalInternship.BehaviorTree;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class GrabItemBehavior
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
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
