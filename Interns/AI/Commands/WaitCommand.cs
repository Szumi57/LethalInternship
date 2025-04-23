using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI.Commands
{
    public class WaitCommand : ICommandAI
    {
        private readonly InternAI ai;

        public WaitCommand(InternAI internAI)
        {
            ai = internAI;
        }

        public void Execute()
        {
            ai.StopMoving();

            // Set where the intern should look
            ai.SetInternLookAt();

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewPriorityCommand(this);
            return;
        }

        public string GetBillboardStateIndicator()
        {
            return "...";
        }

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.Wait;
        }

        public void PlayerHeard(Vector3 noisePosition)
        {
        }

        private void TryPlayCurrentStateVoiceAudio()
        {
        }
    }
}
