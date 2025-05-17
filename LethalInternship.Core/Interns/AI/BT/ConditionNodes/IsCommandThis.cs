using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsCommandThis
    {
        public bool Condition(InternAI ai, EnumCommandTypes commandType)
        {
            return ai.CurrentCommand == commandType;
        }
    }
}
