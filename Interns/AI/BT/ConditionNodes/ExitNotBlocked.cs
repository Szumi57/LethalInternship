namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class ExitNotBlocked
    {
        public bool Condition(InternAI ai)
        {
            if (ai.ClosestEntrance == null)
            {
                Plugin.LogError("ExitNotBlocked Condition, ClosestEntrance is null !");
                return true;
            }

            Plugin.LogDebug($"ai.ClosestEntrance.FindExitPoint() {ai.ClosestEntrance.FindExitPoint()}");
            return ai.ClosestEntrance.FindExitPoint();
        }
    }
}
