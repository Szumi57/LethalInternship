using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class SetGatheringPointAbility : TargetedAbility
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
            TargetingManager.Instance.SetActiveSearch(TargetingManager.TargetType.GatheringPoint);
            InputManager.Instance.StartTargeting(this);
        }

        public override Order? ResolveTarget(TargetData target)
        {
            IPointOfInterest gatheringPOI = InternManager.Instance.GetPointOfInterestOrNewGatheringPoint(target.PointedPointOfInterest == null ? target.RaycastHit.point : target.PointedPointOfInterest.GetPoint());
            Dictionary<Type, IInterestPoint> dictTypeInterestPoint = gatheringPOI.GetDictTypeInterestPoints();
            if (dictTypeInterestPoint.TryGetValue(typeof(GatheringInterestPoint), out var interestPoint))
                return new SetGatheringPointOrder(gatheringPOI);

            PluginLoggerHook.LogError?.Invoke("Target of SetGatheringPointAbility null or gatheringPOI is null !");
            return null;
        }
    }
}
