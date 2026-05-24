using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoToVehicleOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("vehicleController not found !");
                return;
            }

            IPointOfInterest pointOfInterest = InternManager.Instance.GetPointOfInterestOrNewVehiclePoint(vehicleController);
            // Give order
            intern.SetCommandTo(pointOfInterest);
        }
    }
}
