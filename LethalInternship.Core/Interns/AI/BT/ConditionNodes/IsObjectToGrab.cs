namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsObjectToGrab : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Check for object to grab
            if (!ai.AreHandsFree())
            {
                return false;
            }

            GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
            if (grabbableObject == null)
            {
                return false;
            }

            ai.TargetItem = grabbableObject;
            return true;
        }
    }
}
