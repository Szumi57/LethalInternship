using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ContextOrderAbility : TargetedAbility
    {
        protected override void BeginTargeting()
        {
            InputManager.Instance.StartTargeting(this);
            // UI
            //UIManager.Instance.ShowContextHint();
        }

        public override Order? ResolveTarget(TargetData target)
        {
            //if (target.Enemy != null)
            //    return new AttackOrder(target.Enemy);

            //if (target.Item != null)
            //    return new PickupOrder(target.Item);

            if (target.PointOfInterest == null)
            {
                PluginLoggerHook.LogError?.Invoke("Target of ContextOrderAbility null or PointOfInterest is null !");
                return null;
            }

            return new GoToInterestPointOrder(target.PointOfInterest);
        }
    }
}
