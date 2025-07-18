using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsCommandThis : IBTCondition
    {
        private readonly EnumCommandTypes commandType;

        public IsCommandThis(EnumCommandTypes commandType)
        {
            this.commandType = commandType;
        }

        public bool Condition(BTContext context)
        {
            return context.InternAI.CurrentCommand == commandType;
        }
    }
}
