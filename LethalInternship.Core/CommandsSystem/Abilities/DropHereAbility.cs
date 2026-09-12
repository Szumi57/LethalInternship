using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class DropHereAbility : Ability
    {
        private readonly bool dropAll;

        public override bool RequiresTargeting => false;

        public DropHereAbility(bool dropAll, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            this.dropAll = dropAll;
        }

        public override void Activate()
        {
            if (dropAll)
                InternManager.Instance.ExecuteOrder(new DropAllItemsOrder(IdentitiesToOrder));
            else
                InternManager.Instance.ExecuteOrder(new DropItemOrder(IdentitiesToOrder));
        }
    }
}
