using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class UpdateDestCruiser : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("SetNextDestToDropLocation vehicleController not found !");
                return BehaviourTreeStatus.Failure;
            }
            context.PathController.SetNewDestination(new DJKVehiclePoint(vehicleController.transform, $"Cruiser drop location"));

            return BehaviourTreeStatus.Success;
        }
    }
}
