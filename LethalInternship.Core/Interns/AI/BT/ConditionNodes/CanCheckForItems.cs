namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class CanCheckForItems : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            // Check for object to grab
            if (!context.InternAI.AreHandsFree())
            {
                return false;
            }

            return true;
        }
    }
}
