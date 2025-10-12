using LethalInternship.SharedAbstractions.Constants;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromPos : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            if (!context.PathController.IsCurrentPointDestination())
            {
                // Current point is not destination (here position) so : too far
                return true;
            }

            InternAI ai = context.InternAI;

            Vector3 currentPoint = context.PathController.GetCurrentPointPos(ai.transform.position);

            float sqrHorizontalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                return false;
            }

            return true;
        }
    }
}
