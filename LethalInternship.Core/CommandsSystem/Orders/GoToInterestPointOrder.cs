using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoToInterestPointOrder : Order
    {
        private readonly IPointOfInterest pointOfInterest;

        public GoToInterestPointOrder(IPointOfInterest pointOfInterest, IReadOnlyList<IInternIdentity> identities)
                : base(identities)
        {
            this.pointOfInterest = pointOfInterest;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandTo(pointOfInterest);
        }
    }
}
