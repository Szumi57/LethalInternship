using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;

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
            if (!InternManager.Instance.IsGrabbableObjectGrabbable(context.TargetItem, forcePickUp: context.InternAI.CurrentCommand == EnumCommandTypes.GoFetchItem))
            {
                return false;
            }

            return true;
        }
    }
}
