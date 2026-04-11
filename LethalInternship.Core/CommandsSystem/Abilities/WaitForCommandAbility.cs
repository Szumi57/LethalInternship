using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class WaitForCommandAbility : Ability
    {
        public override bool RequiresTargeting => false;

        private bool wait;

        public WaitForCommandAbility(bool wait)
        {
            this.wait = wait;
        }

        public override void Activate()
        {
            if (!this.wait)
            {
                IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesSpawned());
                IdentitySelectionService.Instance.SelectAll();
            }
            InternManager.Instance.ExecuteOrder(new WaitForCommandOrder(wait));
        }
    }
}
