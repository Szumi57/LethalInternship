using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class EnterVehicle : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogError?.Invoke("EnterVehicle action, vehicleController is null !");
                return BehaviourTreeStatus.Failure;
            }

            if (!ai.NpcController.IsControllerInCruiser)
            {
                ai.EnterCruiser(vehicleController);
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
