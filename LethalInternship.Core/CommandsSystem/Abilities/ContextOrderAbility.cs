using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine;

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
            TargetingManager.Instance.SetActiveSearch(TargetingManager.TargetType.Enemy | TargetingManager.TargetType.Item);
            InputManager.Instance.StartTargeting(this);
        }

        public override Order? ResolveTarget(TargetData target)
        {
            Debug.Log($"ResolveTarget {target}");
            if (target.Enemy != null)
                return new AttackOrder(target.Enemy);

            if (target.Item != null)
                return new GoFetchItemOrder(target.Item);

            if (target.PointedPointOfInterest != null)
                return new GoToInterestPointOrder(target.PointedPointOfInterest);

            IPointOfInterest? resolvedPOI = ResolvePointOfInterest(target.RaycastHit);
            if (resolvedPOI != null)
                return new GoToInterestPointOrder(resolvedPOI);

            PluginLoggerHook.LogError?.Invoke("Target of ContextOrderAbility null or PointOfInterest is null !");
            return null;
        }

        private IPointOfInterest? ResolvePointOfInterest(RaycastHit targetHit)
        {
            if (TargetingManager.Instance.IsColliderFromVehicle(targetHit.collider))
            {
                return InternManager.Instance.GetPointOfInterestOrNewVehiclePoint(targetHit.collider.gameObject.GetComponentInParent<VehicleController>());
            }
            else if (TargetingManager.Instance.IsColliderFromShip(targetHit.collider))
            {
                Transform? shipTransform = TargetingManager.Instance.GetParentShip(targetHit.collider.gameObject.transform);
                if (shipTransform != null)
                    return InternManager.Instance.GetPointOfInterestOrNewShipPoint(shipTransform);
            }
            else if (targetHit.point != Vector3.zero)
            {
                return InternManager.Instance.GetPointOfInterestOrNewPositionPoint(targetHit.point);
            }

            return null;
        }


    }
}
