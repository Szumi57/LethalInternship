using LethalInternship.BehaviorTree;
using LethalInternship.Constants;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class ExitVehicle
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                Plugin.LogError("EnterVehicle action, vehicleController is null !");
                return BehaviourTreeStatus.Failure;
            }

            Vector3 entryPointInternCruiser = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomEntryPosCruiser();

            // Exit vehicle cruiser
            ai.SyncTeleportInternVehicle(entryPointInternCruiser, enteringVehicle: false, vehicleController);
            vehicleController.SetVehicleCollisionForPlayer(true, ai.NpcController.Npc);

            return BehaviourTreeStatus.Success;
        }

        private Vector3 GetNextRandomEntryPosCruiser()
        {
            float x = Random.Range(Const.POS1_ENTRY_INTERN_CRUISER.x, Const.POS2_ENTRY_INTERN_CRUISER.x);

            return new Vector3(x, Const.POS1_ENTRY_INTERN_CRUISER.y, Const.POS1_ENTRY_INTERN_CRUISER.z);
        }
    }
}
