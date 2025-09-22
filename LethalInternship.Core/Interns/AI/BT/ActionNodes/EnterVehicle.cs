using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

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

            // Teleport to cruiser and enter vehicle
            // Place intern in random spot
            Vector3 internPassengerPos = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomInCruiserPos();
            ai.SyncTeleportInternVehicle(internPassengerPos, enteringVehicle: true, vehicleController);
            PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} EnterVehicle !");

            // random rotation
            float angleRandom = Random.Range(-180f, 180f);
            ai.NpcController.UpdateNowTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * ai.NpcController.Npc.thisController.transform.forward);

            // Crouch or not
            float crouchRancom = Random.Range(0f, 1f);
            if (crouchRancom > 0.5f
                && !ai.NpcController.Npc.isCrouching)
            {
                ai.NpcController.OrderToToggleCrouch();
            }

            return BehaviourTreeStatus.Success;
        }

        private Vector3 GetNextRandomInCruiserPos()
        {
            float x = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.x, Const.SECOND_CORNER_INTERN_IN_CRUISER.x);
            float y = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.y, Const.SECOND_CORNER_INTERN_IN_CRUISER.y);
            float z = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.z, Const.SECOND_CORNER_INTERN_IN_CRUISER.z);

            return new Vector3(x, y, z);
        }
    }
}
