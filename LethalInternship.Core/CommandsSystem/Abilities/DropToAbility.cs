using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class DropToAbility : Ability
    {
        private readonly EnumCommandTypes dropCommand;

        public override bool RequiresTargeting => false;

        public DropToAbility(EnumCommandTypes dropCommand, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            this.dropCommand = dropCommand;
        }

        public override void Activate()
        {
            switch (dropCommand)
            {
                case EnumCommandTypes.None:
                    break;
                case EnumCommandTypes.DropAllItemsToShip:
                    InternManager.Instance.ExecuteOrder(new DropToShipOrder(IdentitiesToOrder));
                    break;
                case EnumCommandTypes.DropAllItemsOnGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new DropToGatheringPointOrder(IdentitiesToOrder));
                    break;
                case EnumCommandTypes.DropAllItemsInCruiser:
                    InternManager.Instance.ExecuteOrder(new DropToCruiserOrder(IdentitiesToOrder));
                    break;
            }
        }
    }
}
