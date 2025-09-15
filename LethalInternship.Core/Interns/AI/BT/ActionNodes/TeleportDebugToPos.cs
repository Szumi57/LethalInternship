using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class TeleportDebugToPos : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            PluginLoggerHook.LogWarning?.Invoke("Teleporting directly, maybe a problem occured.");

            InternAI ai = context.InternAI;

            ai.SyncTeleportIntern(context.PathController.GetCurrentPoint(ai.transform.position), !ai.isOutside, isUsingEntrance: false);
            return BehaviourTreeStatus.Success;
        }
    }
}
