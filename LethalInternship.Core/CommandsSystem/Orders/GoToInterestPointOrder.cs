using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoToInterestPointOrder : Order
    {
        private IPointOfInterest PointOfInterest;

        public GoToInterestPointOrder(IPointOfInterest pointOfInterest)
        {
            PointOfInterest = pointOfInterest;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandTo(PointOfInterest);
        }
    }
}
