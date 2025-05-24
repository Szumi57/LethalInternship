using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromVehicle : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogError?.Invoke("TooFarFromVehicle Condition, vehicleController is null !");
                return false;
            }

            PluginLoggerHook.LogDebug?.Invoke($"{(ai.NpcController.Npc.transform.position - vehicleController.transform.position).magnitude}");
            if ((ai.NpcController.Npc.transform.position - vehicleController.transform.position).sqrMagnitude < Const.DISTANCE_TO_CRUISER * Const.DISTANCE_TO_CRUISER)
            {
                return false;
            }

            return true;
        }
    }
}
