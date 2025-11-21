namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class CanHoldNewItem : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            // Check for object to grab
            if (!context.InternAI.CanHoldNewItem())
            {
                return false;
            }

            return true;
        }
    }
}
