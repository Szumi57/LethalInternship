using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class DropItemOrder : Order
    {
        public DropItemOrder(IReadOnlyList<IInternIdentity> identities) : base(identities)
        {
        }

        public override void ApplyTo(IInternAI intern)
        {
            GrabbableObject? grabbableObject = intern.GetCurrentlyHeldItem();
            if (grabbableObject != null)
            {
                intern.DropItem(grabbableObject);
            }
        }
    }
}
