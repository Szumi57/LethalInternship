using GameNetcodeStuff;
using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class UpdateLastKnownPos : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            float sqrHorizontalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    && sqrVerticalDistanceWithTarget < Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
            {
                ai.TargetLastKnownPosition = ai.targetPlayer.transform.position;
            }
            else
            {
                PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (target != null)
                {
                    ai.TargetLastKnownPosition = target.transform.position;
                }
                else
                {
                    // Maybe target got inside/outside
                    if ((ai.isOutside && ai.targetPlayer.isInsideFactory)
                        || (!ai.isOutside && !ai.targetPlayer.isInsideFactory))
                    {
                        ai.TargetLastKnownPosition = ai.targetPlayer.transform.position;
                    }
                }
            }

            PluginLoggerHook.LogDebug?.Invoke($"targetPlayer pos {ai.TargetLastKnownPosition}");
            return BehaviourTreeStatus.Success;
        }
    }
}
