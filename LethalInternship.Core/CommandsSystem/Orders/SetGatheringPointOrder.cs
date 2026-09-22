using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class SetGatheringPointOrder : Order
    {
        private IPointOfInterest newGatheringPoint;

        public SetGatheringPointOrder(IPointOfInterest newGatheringPoint, IReadOnlyList<IInternIdentity> identities)
                : base(identities)
        {
            this.newGatheringPoint = newGatheringPoint;
        }

        public override void ApplyTo(IInternAI intern)
        {
            InternManager.Instance.SetGatheringPoint(newGatheringPoint);
        }
    }
}
