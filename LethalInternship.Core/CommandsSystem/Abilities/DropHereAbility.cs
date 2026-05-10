using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class DropHereAbility : Ability
    {
        private readonly bool dropAll;

        public override bool RequiresTargeting => false;

        public DropHereAbility(bool dropAll)
        {
            this.dropAll = dropAll;
        }

        public override void Activate()
        {
            if (dropAll)
                InternManager.Instance.ExecuteOrder(new DropAllItemsOrder());
            else
                InternManager.Instance.ExecuteOrder(new DropItemOrder());
        }
    }
}
