namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class NotValidEntrance : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            return context.InternAI.ClosestEntrance == null;
        }
    }
}
