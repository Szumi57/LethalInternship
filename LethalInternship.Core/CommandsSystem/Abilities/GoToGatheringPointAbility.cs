using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class GoToGatheringPointAbility : Ability
    {
        public override bool RequiresTargeting => false;

        public override void Activate()
        {
            if (InternManager.Instance.GatheringPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("GoToGatheringPointAbility no gathering point set !");
                return;
            }

            InternManager.Instance.ExecuteOrder(new GoToInterestPointOrder(InternManager.Instance.GatheringPoint));
        }
    }
}
