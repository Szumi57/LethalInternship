using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class SetGatheringPointOrder : Order
    {
        private IPointOfInterest newGatheringPoint;

        public SetGatheringPointOrder(IPointOfInterest newGatheringPoint)
        {
            this.newGatheringPoint = newGatheringPoint;
        }

        public override void ApplyTo(IInternAI intern)
        {
            InternManager.Instance.SetGatheringPoint(newGatheringPoint);
        }
    }
}
