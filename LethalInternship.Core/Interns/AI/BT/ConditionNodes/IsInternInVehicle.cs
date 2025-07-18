namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsInternInVehicle : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            return context.InternAI.NpcController.IsControllerInCruiser;
        }
    }
}
