using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class NextPositionIsAfterEntrance
    {
        public bool Condition(InternAI ai, Vector3 nextPos)
        {
            if (ai.isOutside && nextPos.y < -80f)
            {
                return true;
            }

            if (!ai.isOutside && nextPos.y >= -80f)
            {
                return true;
            }

            return false;
        }
    }
}
