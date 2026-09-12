using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class Ability
    {
        public IReadOnlyList<IInternIdentity> IdentitiesToOrder { get; }

        protected Ability(IEnumerable<IInternIdentity> identities)
        {
            IdentitiesToOrder = identities.ToArray();
        }

        public abstract bool RequiresTargeting { get; }
        public abstract void Activate();
    }
}
