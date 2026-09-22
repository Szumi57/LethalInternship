using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class AreFreeSlotsAvailable : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Check for object to grab
            if (!ai.AreFreeSlotsAvailable())
            {
                if (ai.CurrentCommand == EnumCommandTypes.GoFetchItem)
                    ai.SetCommandFeedback(EnumTempCommandFeedback.NoFreeSlotsAvailable);

                return false;
            }

            return true;
        }
    }
}
