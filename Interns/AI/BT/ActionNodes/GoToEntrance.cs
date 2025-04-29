using LethalInternship.BehaviorTree;
using LethalInternship.Constants;
using UnityEngine;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class GoToEntrance
    {
        public BehaviourTreeStatus Action(InternAI ai, bool canRun = true)
        {
            if (ai.ClosestEntrance == null)
            {
                Plugin.LogError("TooFarFromEntrance Action, ClosestPosOfEntrance is null !");
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

            ai.SetDestinationToPositionInternAI(ai.ClosestEntrance.entrancePoint.position);
            ai.OrderAgentAndBodyMoveToDestination();

            return BehaviourTreeStatus.Success;
        }
    }
}
