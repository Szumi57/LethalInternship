namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class IsTargetInVehicle
    {
        public bool Condition(InternAI ai)
        {
            if (ai.targetPlayer == null)
            {
                Plugin.LogError("IsTargetInVehicle condition, targetPlayer is null !");
                return false;
            }

            return ai.targetPlayer.inVehicleAnimation;
        }
    }
}
