using LethalInternship.SharedAbstractions.Enums;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsCommandThis : IBTCondition
    {
        private readonly HashSet<EnumCommandTypes> commandTypes = new HashSet<EnumCommandTypes>();

        public IsCommandThis(EnumCommandTypes commandType)
        {
            this.commandTypes.Add(commandType);
        }

        public IsCommandThis(IEnumerable<EnumCommandTypes> commandTypes)
        {
            foreach (EnumCommandTypes commandType in commandTypes)
                this.commandTypes.Add(commandType);
        }

        public bool Condition(BTContext context)
        {
            return commandTypes.Contains(context.InternAI.CurrentCommand);
        }
    }
}
