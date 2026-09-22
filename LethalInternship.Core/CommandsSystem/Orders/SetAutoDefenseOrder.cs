using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class SetAutoDefenseOrder : Order
    {
        private bool _autoDefense;

        public SetAutoDefenseOrder(bool autoDefense, IReadOnlyList<IInternIdentity> identities)
                : base(identities)
        {
            _autoDefense = autoDefense;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetAutoDefenseModeServerRpc(_autoDefense);
        }
    }
}
