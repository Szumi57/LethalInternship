using GameNetcodeStuff;
using LethalInternship.BehaviorTree;
using LethalInternship.Constants;
using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class UpdateLastKnownPos
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
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

            Plugin.LogDebug($"targetPlayer pos {ai.TargetLastKnownPosition}");
            return BehaviourTreeStatus.Success;
        }
    }
}
