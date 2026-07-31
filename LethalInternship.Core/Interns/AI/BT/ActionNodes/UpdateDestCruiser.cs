using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class UpdateDestCruiser : IBTAction
    {
        private DJKVehiclePoint _vehiclePoint = new DJKVehiclePoint("Cruiser drop location");

        public BehaviourTreeStatus Action(BTContext context)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("SetNextDestToDropLocation vehicleController not found !");
                return BehaviourTreeStatus.Failure;
            }
            _vehiclePoint.Transform = vehicleController.transform;
            context.FinalDestination = _vehiclePoint;
            context.PathfindingContext.SetDestination(context.FinalDestination.Clone(InternManager.Instance.Pools));

            return BehaviourTreeStatus.Success;
        }
    }
}
