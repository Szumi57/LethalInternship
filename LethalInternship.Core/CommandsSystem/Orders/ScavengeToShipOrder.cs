using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ScavengeToShipOrder : Order
    {
        public ScavengeToShipOrder(IReadOnlyList<IInternIdentity> identities) : base(identities)
        {
        }

        public override void ApplyTo(IInternAI intern)
        {
            // Give order
            intern.SetCommandToScavengingToShip();
        }
    }
}
