using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class SetAutoDefenseAbility : Ability
    {
        private bool _autoDefense;

        public override bool RequiresTargeting => false;

        public SetAutoDefenseAbility(bool autoDefense)
        {
            _autoDefense = autoDefense;
        }

        public override void Activate()
        {
            InternManager.Instance.ExecuteOrder(new SetAutoDefenseOrder(_autoDefense));
        }
    }
}
