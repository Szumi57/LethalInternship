using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class DropItem : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            IInternAI ai = context.InternAI;

            if (ai.AreHandsFree())
            {
                PluginLoggerHook.LogError?.Invoke("DropItem action failed, no item held !");
                return BehaviourTreeStatus.Failure;
            }

            ai.DropAllItems();

            return BehaviourTreeStatus.Success;
        }
    }
}
