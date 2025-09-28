using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;
using UnityEngine.AI;

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
                PluginLoggerHook.LogError?.Invoke("ExitVehicle action, vehicleController is null !");
                return BehaviourTreeStatus.Failure;
            }

            Vector3 exitPointInternCruiser = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomEntryPosCruiser();
            NavMeshHit hitEnd;
            if (NavMesh.SamplePosition(exitPointInternCruiser, out hitEnd, 2f, NavMesh.AllAreas))
            {
                PluginLoggerHook.LogDebug?.Invoke("-> ExitVehicle use SamplePosition");
                exitPointInternCruiser = hitEnd.position;
            }

            // Exit vehicle cruiser
            ai.SyncTeleportInternVehicle(exitPointInternCruiser, enteringVehicle: false, vehicleController);
            vehicleController.SetVehicleCollisionForPlayer(true, ai.NpcController.Npc);

            if (ai.NpcController.Npc.isCrouching)
            {
                ai.NpcController.OrderToToggleCrouch();
            }

            return BehaviourTreeStatus.Success;
        }

        private Vector3 GetNextRandomEntryPosCruiser()
        {
            float x = Random.Range(Const.POS1_ENTRY_INTERN_CRUISER.x, Const.POS2_ENTRY_INTERN_CRUISER.x);

            return new Vector3(x, Const.POS1_ENTRY_INTERN_CRUISER.y, Const.POS1_ENTRY_INTERN_CRUISER.z);
        }
    }
}
