using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ScavengeToDropLocationAbility : Ability
    {
        private readonly EnumCommandTypes dropLocation;

        public override bool RequiresTargeting => false;

        public ScavengeToDropLocationAbility(EnumCommandTypes dropLocation)
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
                    InternManager.Instance.ExecuteOrder(new ScavengeToShipOrder());
                    break;
                case EnumCommandTypes.ScavengingToCruiser:
                    InternManager.Instance.ExecuteOrder(new ScavengeToCruiserOrder());
                    break;
                case EnumCommandTypes.ScavengingToGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new ScavengeToGatheringPointOrder());
                    break;
            }
        }
    }
}
