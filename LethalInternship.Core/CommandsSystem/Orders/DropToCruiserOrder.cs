using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class DropToCruiserOrder : Order
    {
        public DropToCruiserOrder(IReadOnlyList<IInternIdentity> identities) : base(identities)
        {
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToDropToCruiser();
        }
    }
}
