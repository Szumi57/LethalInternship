namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class IsObjectToGrab
    {
        public bool Condition(InternAI internAI)
        {
            // Check for object to grab
            if (!internAI.AreHandsFree())
            {
                return false;
            }

            GrabbableObject? grabbableObject = internAI.LookingForObjectToGrab();
            if (grabbableObject == null)
            {
                return false;
            }

            internAI.TargetItem = grabbableObject;
            return true;
        }
    }
}
