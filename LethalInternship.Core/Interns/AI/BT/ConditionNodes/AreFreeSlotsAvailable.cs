namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class AreFreeSlotsAvailable : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            // Check for object to grab
            if (!context.InternAI.AreFreeSlotsAvailable())
            {
                return false;
            }

            return true;
        }
    }
}
