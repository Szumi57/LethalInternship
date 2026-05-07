using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ScavengeToDropLocationAbility : Ability
    {
        private readonly EnumLocationGoalTypes dropLocation;

        public override bool RequiresTargeting => false;

        public ScavengeToDropLocationAbility(EnumLocationGoalTypes dropLocation)
        {
            this.dropLocation = dropLocation;
        }

        public override void Activate()
        {
            switch (dropLocation)
            {
                case EnumLocationGoalTypes.None:
                    break;
                case EnumLocationGoalTypes.Ship:
                    InternManager.Instance.ExecuteOrder(new ScavengeToShipOrder());
                    break;
                case EnumLocationGoalTypes.Cruiser:
                    InternManager.Instance.ExecuteOrder(new ScavengeToCruiserOrder());
                    break;
                case EnumLocationGoalTypes.GatheringPoint:
                    InternManager.Instance.ExecuteOrder(new ScavengeToGatheringPointOrder());
                    break;
            }
        }
    }
}
