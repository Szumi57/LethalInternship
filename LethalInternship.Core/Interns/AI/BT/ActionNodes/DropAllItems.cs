using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class DropAllItems : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            IInternAI ai = context.InternAI;

            if (ai.AreHandsFree())
            {
                PluginLoggerHook.LogError?.Invoke("DropItem action failed, no item held !");
                return BehaviourTreeStatus.Failure;
            }

            EnumOptionsGetItems options = EnumOptionsGetItems.IgnoreWeapon;
            ai.DropAllItems(options);

            return BehaviourTreeStatus.Success;
        }
    }
}
