using LethalInternship.BehaviorTree;
using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class TeleportDebugToPos
    {
        public BehaviourTreeStatus Action(InternAI ai, Vector3 pos)
        {
            Plugin.LogWarning("Teleporting directly, maybe a problem occured.");

            ai.SyncTeleportIntern(pos, !ai.isOutside, isUsingEntrance: false);
            return BehaviourTreeStatus.Success;
        }
    }
}
