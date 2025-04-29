using LethalInternship.Enums;

namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class IsCommandThis
    {
        public bool Condition(InternAI ai, EnumCommandTypes commandType)
        {
            return ai.CurrentCommand == commandType;
        }
    }
}
