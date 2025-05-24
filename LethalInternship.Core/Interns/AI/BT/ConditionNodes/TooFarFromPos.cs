using LethalInternship.SharedAbstractions.Constants;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromPos : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            float sqrHorizontalDistance = Vector3.Scale(ai.NextPos - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(ai.NextPos - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                return false;
            }

            return true;
        }

        
    }
}
