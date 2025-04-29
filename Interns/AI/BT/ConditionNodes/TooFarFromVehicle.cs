using LethalInternship.Constants;
using LethalInternship.Managers;

namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromVehicle
    {
        public bool Condition(InternAI ai)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                Plugin.LogError("TooFarFromVehicle Condition, vehicleController is null !");
                return false;
            }

            Plugin.LogDebug($"{(ai.NpcController.Npc.transform.position - vehicleController.transform.position).magnitude}");
            if ((ai.NpcController.Npc.transform.position - vehicleController.transform.position).sqrMagnitude < Const.DISTANCE_TO_CRUISER * Const.DISTANCE_TO_CRUISER)
            {
                return false;
            }

            return true;
        }
    }
}
