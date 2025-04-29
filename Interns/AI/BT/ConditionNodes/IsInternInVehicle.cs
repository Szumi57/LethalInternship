namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class IsInternInVehicle
    {
        public bool Condition(InternAI ai)
        {
            return ai.NpcController.IsControllerInCruiser;
        }
    }
}
