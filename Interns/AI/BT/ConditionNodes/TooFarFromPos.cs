using LethalInternship.Constants;
using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromPos
    {
        public bool Condition(InternAI ai, Vector3 targetPosition)
        {
            float sqrHorizontalDistance = Vector3.Scale(targetPosition - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(targetPosition - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                return false;
            }

            return true;
        }

        
    }
}
