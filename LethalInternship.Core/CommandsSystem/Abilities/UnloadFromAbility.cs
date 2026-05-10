using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class UnloadFromAbility : Ability
    {
        private readonly EnumCommandTypes unloadCommand;

        public override bool RequiresTargeting => false;

        public UnloadFromAbility(EnumCommandTypes dropCommand)
        {
            this.unloadCommand = dropCommand;
        }

        public override void Activate()
        {
            switch (unloadCommand)
            {
                case EnumCommandTypes.None:
                    break;
                case EnumCommandTypes.UnloadCruiser:
                    InternManager.Instance.ExecuteOrder(new UnloadCruiserOrder());
                    break;
                case EnumCommandTypes.UnloadGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new UnloadGatheringPointOrder());
                    break;
            }
        }
    }
}
