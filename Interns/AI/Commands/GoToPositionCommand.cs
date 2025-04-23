using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI.Commands
{
    public class GoToPositionCommand : ICommandAI
    {
        private readonly InternAI ai;
        private NpcController Controller { get { return ai.NpcController; } }

        public GoToPositionCommand(InternAI internAI)
        {
            ai = internAI;
        }

        public void Execute()
        {
            if (ai.CommandPoint == null)
            {
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return;
            }

            float sqrHorizontalDistanceWithPoint = Vector3.Scale(ai.CommandPoint.Value - Controller.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistanceWithPoint = Vector3.Scale(ai.CommandPoint.Value - Controller.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistanceWithPoint < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistanceWithPoint < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.QueueNewPriorityCommand(new WaitCommand(ai));
                return;
            }

            ai.SetDestinationToPositionInternAI(ai.CommandPoint.Value);
            ai.OrderAgentAndBodyMoveToDestination();

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewPriorityCommand(this);
            return;
        }

        public string GetBillboardStateIndicator()
        {
            return "-->";
        }

        public void PlayerHeard(Vector3 noisePosition)
        {
        }

        private void TryPlayCurrentStateVoiceAudio()
        {
        }

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.GoToPosition;
        }
    }
}
