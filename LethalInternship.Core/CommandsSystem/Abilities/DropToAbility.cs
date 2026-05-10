using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class DropToAbility : Ability
    {
        private readonly EnumCommandTypes dropCommand;

        public override bool RequiresTargeting => false;

        public DropToAbility(EnumCommandTypes dropCommand)
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
                    InternManager.Instance.ExecuteOrder(new DropToShipOrder());
                    break;
                case EnumCommandTypes.DropAllItemsOnGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new DropToGatheringPointOrder());
                    break;
                case EnumCommandTypes.DropAllItemsInCruiser:
                    InternManager.Instance.ExecuteOrder(new DropToCruiserOrder());
                    break;
            }
        }
    }
}
