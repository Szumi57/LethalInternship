using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoToShipOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            Transform? shipTransform = InternManager.Instance.ShipTransform;
            if (shipTransform == null)
            {
                PluginLoggerHook.LogError?.Invoke("InputManager GiveOrderGoToShip shipTransform not found !");
                return;
            }

            IPointOfInterest pointOfInterest = InternManager.Instance.GetPointOfInterestOrShipInterestPoint(shipTransform);
            // Give order
            intern.SetCommandTo(pointOfInterest);
        }
    }
}
