using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class TeleportDebugToPos
    {
        public BehaviourTreeStatus Action(InternAI ai, Vector3 pos)
        {
            PluginLoggerHook.LogWarning?.Invoke("Teleporting directly, maybe a problem occured.");

            ai.SyncTeleportIntern(pos, !ai.isOutside, isUsingEntrance: false);
            return BehaviourTreeStatus.Success;
        }
    }
}
