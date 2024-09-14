using LethalInternship.Enums;

namespace LethalInternship.AI.AIStates
{
    internal class BrainDeadState : AIState
    {
        public BrainDeadState(AIState oldState) : base(oldState)
        {
            CurrentState = EnumAIStates.BrainDead;
        }

        public override void DoAI()
        {
            ai.StopMoving();
        }
    }
}
