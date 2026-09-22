using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ScavengeToDropLocationAbility : Ability
    {
        private readonly EnumCommandTypes dropLocation;

        public override bool RequiresTargeting => false;

        public ScavengeToDropLocationAbility(EnumCommandTypes dropLocation, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            this.dropLocation = dropLocation;
        }

        public override void Activate()
        {
            switch (dropLocation)
            {
                case EnumCommandTypes.None:
                    break;
                case EnumCommandTypes.ScavengingToShip:
                    InternManager.Instance.ExecuteOrder(new ScavengeToShipOrder(IdentitiesToOrder));
                    break;
                case EnumCommandTypes.ScavengingToCruiser:
                    InternManager.Instance.ExecuteOrder(new ScavengeToCruiserOrder(IdentitiesToOrder));
                    break;
                case EnumCommandTypes.ScavengingToGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new ScavengeToGatheringPointOrder(IdentitiesToOrder));
                    break;
            }
        }
    }
}
