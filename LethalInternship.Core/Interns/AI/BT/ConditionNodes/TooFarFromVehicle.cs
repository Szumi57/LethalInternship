using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

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

            Vector3 internPos = new Vector3(ai.NpcController.Npc.transform.position.x, 0f, ai.NpcController.Npc.transform.position.z);
            Vector3 VehiclePos = new Vector3(vehicleController.transform.position.x, 0f, vehicleController.transform.position.z);
            PluginLoggerHook.LogDebug?.Invoke($"{(internPos - VehiclePos).magnitude}");
            if ((internPos - VehiclePos).sqrMagnitude < Const.DISTANCE_TO_CRUISER * Const.DISTANCE_TO_CRUISER)
            {
                return false;
            }

            return true;
        }
    }
}
