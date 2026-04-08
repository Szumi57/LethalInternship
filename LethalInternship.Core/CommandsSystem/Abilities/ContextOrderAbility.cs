using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ContextOrderAbility : TargetedAbility
    {
        public override HashSet<GameAction> NotInterruptingActions => notInterruptingActions;
        public override HashSet<GameAction> SubmitActions => submitActions;


        static readonly HashSet<GameAction> notInterruptingActions = new HashSet<GameAction>()
                                                    {
                                                        GameAction.Look,
                                                        GameAction.Move,
                                                        GameAction.Jump,
                                                        GameAction.Sprint,
                                                        GameAction.Crouch
                                                    };
        static readonly HashSet<GameAction> submitActions = new HashSet<GameAction>()
                                                    {
                                                        GameAction.Use,
                                                        GameAction.ActivateItem
                                                    };

        protected override void BeginTargeting()
        {
            InputManager.Instance.StartTargeting(this);
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
