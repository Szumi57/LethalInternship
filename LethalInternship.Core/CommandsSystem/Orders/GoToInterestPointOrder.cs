using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoToInterestPointOrder : Order
    {
        private readonly IPointOfInterest pointOfInterest;

        public GoToInterestPointOrder(IPointOfInterest pointOfInterest)
        {
            this.pointOfInterest = pointOfInterest;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandTo(pointOfInterest);
        }
    }
}
