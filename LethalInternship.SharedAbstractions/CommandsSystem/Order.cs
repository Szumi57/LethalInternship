using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class Order
    {
        public IReadOnlyList<IInternIdentity> IdentitiesToOrder { get; }

        protected Order(IReadOnlyList<IInternIdentity> identities)
        {
            IdentitiesToOrder = identities;
        }

        public abstract void ApplyTo(IInternAI intern);
    }
}
