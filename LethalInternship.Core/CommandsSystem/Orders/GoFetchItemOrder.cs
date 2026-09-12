using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoFetchItemOrder : Order
    {
        private readonly GrabbableObject itemToFetch;

        public GoFetchItemOrder(GrabbableObject item, IReadOnlyList<IInternIdentity> identities)
                : base(identities)
        {
            itemToFetch = item;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToFetchItem(itemToFetch);
        }
    }
}
