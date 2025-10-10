namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsLastKnownPositionValid : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            return context.TargetLastKnownPosition != null;
        }
    }
}
