namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class NextPositionIsAfterEntrance : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.isOutside && ai.NextPos.y < -80f)
            {
                return true;
            }

            if (!ai.isOutside && ai.NextPos.y >= -80f)
            {
                return true;
            }

            return false;
        }
    }
}
