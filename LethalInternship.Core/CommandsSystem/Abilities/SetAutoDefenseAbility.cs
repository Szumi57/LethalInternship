using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class SetAutoDefenseAbility : Ability
    {
        private bool _autoDefense;

        public override bool RequiresTargeting => false;

        public SetAutoDefenseAbility(bool autoDefense, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            _autoDefense = autoDefense;
        }

        public override void Activate()
        {
            InternManager.Instance.ExecuteOrder(new SetAutoDefenseOrder(_autoDefense, IdentitiesToOrder));
        }
    }
}
