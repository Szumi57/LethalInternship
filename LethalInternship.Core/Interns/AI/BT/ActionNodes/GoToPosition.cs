using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToPosition : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.CanRun)
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

            ai.SetDestinationToPositionInternAI(ai.NextPos);
            ai.OrderAgentAndBodyMoveToDestination();

            return BehaviourTreeStatus.Success;
        }
    }
}
