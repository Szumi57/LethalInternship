using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class TakeEntrance : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.ClosestEntrance == null)
            {
                PluginLoggerHook.LogError?.Invoke("TakeEntrance Action, ClosestEntrance is null !");
                return BehaviourTreeStatus.Failure;
            }

            if (ai.ClosestEntrance.exitPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("TakeEntrance Action, ClosestEntrance.exitPoint is null !");
                return BehaviourTreeStatus.Failure;
            }

            ai.SyncTeleportIntern(ai.ClosestEntrance.exitPoint.position, !ai.isOutside, true);
            return BehaviourTreeStatus.Success;
        }
    }
}
