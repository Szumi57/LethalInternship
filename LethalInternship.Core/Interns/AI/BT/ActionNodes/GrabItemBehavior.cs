using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

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

            if (context.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("GrabItemBehavior action, TargetItem is null");
                return BehaviourTreeStatus.Failure;
            }

            ai.GrabItemServerRpc(context.TargetItem.NetworkObject, itemGiven: false);
            context.TargetItem = null;
            return BehaviourTreeStatus.Success;
        }
    }
}
