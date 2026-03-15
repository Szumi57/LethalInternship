using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class FollowMeAbility : Ability
    {
        public override bool RequiresTargeting => false;

        public override void Activate()
        {
            InternManager.Instance.ExecuteOrder(new FollowMeOrder());
        }
    }
}
