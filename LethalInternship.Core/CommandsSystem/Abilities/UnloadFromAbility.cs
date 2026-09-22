using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class UnloadFromAbility : Ability
    {
        private readonly EnumCommandTypes unloadCommand;

        public override bool RequiresTargeting => false;

        public UnloadFromAbility(EnumCommandTypes dropCommand, IEnumerable<IInternIdentity> identities) : base(identities)
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
                    InternManager.Instance.ExecuteOrder(new UnloadCruiserOrder(IdentitiesToOrder));
                    break;
                case EnumCommandTypes.UnloadGatheringPoint:
                    InternManager.Instance.ExecuteOrder(new UnloadGatheringPointOrder(IdentitiesToOrder));
                    break;
            }
        }
    }
}
