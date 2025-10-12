using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CancelScavengingIf : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.nbItemsToCheck == 0)
            {
                ai.SetCommandToFollowPlayer();
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
