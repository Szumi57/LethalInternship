using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class NextPositionIsAfterEntrance : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;
            Vector3 nextPoint = ai.PointOfInterest?.GetPoint() ?? ai.NextPos;

            if (ai.isOutside && nextPoint.y < -80f)
            {
                return true;
            }

            if (!ai.isOutside && nextPoint.y >= -80f)
            {
                return true;
            }

            return false;
        }
    }
}
