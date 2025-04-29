namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class NotValidEntrance
    {
        public bool Condition(InternAI ai)
        {
            return ai.ClosestEntrance == null;
        }
    }
}
