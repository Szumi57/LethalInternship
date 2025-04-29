namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class IsLastKnownPositionValid
    {
        public bool Condition(InternAI ai)
        {
            return ai.TargetLastKnownPosition != null;
        }
    }
}
