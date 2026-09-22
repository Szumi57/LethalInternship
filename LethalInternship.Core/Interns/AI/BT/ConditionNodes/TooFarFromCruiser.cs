using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromCruiser : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.NpcController.IsControllerInCruiser)
            {
                // Close enough
                return false;
            }

            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogDebug?.Invoke("TooFarFromCruiser vehicleController not found !");
                return false;
            }

            // Distance with cruiser (not with DJKPoint of cuiser)
            Vector3 internPos = new Vector3(ai.NpcController.Npc.transform.position.x, 0f, ai.NpcController.Npc.transform.position.z);
            Vector3 VehiclePos = new Vector3(vehicleController.transform.position.x, 0f, vehicleController.transform.position.z);
            //PluginLoggerHook.LogDebug?.Invoke($"TooFarFromVehicle ? {(internPos - VehiclePos).magnitude} ({Const.DISTANCE_TO_CRUISER})");
            if ((internPos - VehiclePos).sqrMagnitude < Const.DISTANCE_TO_CRUISER * Const.DISTANCE_TO_CRUISER)
            {
                // Close enough
                return false;
            }

            // Too far
            return true;
        }
    }
}
