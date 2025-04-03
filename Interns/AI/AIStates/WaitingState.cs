using LethalInternship.Enums;
using LethalInternship.Interns.AI;
using LethalInternship.Utils;
using UnityEngine;

namespace LethalInternship.Interns.AI.AIStates
{
    public class WaitingState : AIState
    {
        private Vector3 waitingDestination;

        public WaitingState(InternAI ai, Vector3 destination) : base(ai)
        {
            CurrentState = EnumAIStates.Waiting;
            waitingDestination = destination;
        }

        public override void DoAI()
        {
            ai.SetDestinationToPositionInternAI(waitingDestination);
            DrawUtil.DrawWhiteLine(ai.LineRendererUtil.GetLineRenderer(), new Ray(ai.transform.position + Vector3.up, ai.destination - (ai.transform.position + Vector3.up)), (ai.destination - (ai.transform.position + Vector3.up)).magnitude);
            ai.NpcController.OrderToMove();
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            ai.InternIdentity.Voice.StopAudioFadeOut();
        }
    }
}
