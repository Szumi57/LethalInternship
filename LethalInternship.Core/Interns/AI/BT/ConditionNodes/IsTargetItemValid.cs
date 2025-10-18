namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsTargetItemValid : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            if (context.TargetItem == null)
            {
                return false;
            }
            if (!context.InternAI.IsGrabbableObjectGrabbable(context.TargetItem))
            {
                return false;
            }

            return true;
        }
    }
}
