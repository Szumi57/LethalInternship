using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class GoToVehicleAbility : Ability
    {
        public GoToVehicleAbility(IEnumerable<IInternIdentity> identities) : base(identities)
        {
        }

        public override bool RequiresTargeting => false;

        public override void Activate()
        {
            InternManager.Instance.ExecuteOrder(new GoToVehicleOrder(IdentitiesToOrder));
        }
    }
}
