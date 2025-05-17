using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class DropItem
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            if (ai.AreHandsFree())
            {
                PluginLoggerHook.LogError?.Invoke("DropItem action failed, no item held !");
                return BehaviourTreeStatus.Failure;
            }

            ai.DropItem();

            return BehaviourTreeStatus.Success;
        }
    }
}
