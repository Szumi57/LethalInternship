using LethalInternship.Enums;

namespace LethalInternship.AI.AIStates
{
    public class BrainDeadState : AIState
    {
        public BrainDeadState(InternAI ai) : base(ai)
        {
            CurrentState = EnumAIStates.BrainDead;
        }

        public override void DoAI()
        {
            ai.StopMoving();
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            ai.InternIdentity.Voice.StopAudioFadeOut();
        }
    }
}
