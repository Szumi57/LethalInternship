using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToVehicle
    {
        public BehaviourTreeStatus Action(InternAI ai, bool canRun = true)
        {
            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                PluginLoggerHook.LogError?.Invoke("GoToVehicle action, vehicleController is null !");
                return BehaviourTreeStatus.Failure;
            }

            if (canRun)
            {
                float sqrHorizontalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
                float sqrVerticalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;

                if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                     || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
                {
                    ai.NpcController.OrderToSprint();
                }
                else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
                {
                    ai.NpcController.OrderToStopSprint();
                }
            }
            ai.NpcController.OrderToLookForward();

            ai.SetDestinationToPositionInternAI(ai.ChooseClosestNodeToPosition(vehicleController.transform.position, avoidLineOfSight: false, offset: 0).position);
            ai.OrderAgentAndBodyMoveToDestination();

            return BehaviourTreeStatus.Success;
        }
    }
}
