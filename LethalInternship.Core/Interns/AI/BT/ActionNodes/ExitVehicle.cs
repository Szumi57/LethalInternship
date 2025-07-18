using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class ExitVehicle : IBTAction
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
