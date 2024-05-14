using LethalInternship.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.AI.States
{
    internal class StuckState : State
    {
        private static readonly EnumStates STATE = EnumStates.Stuck;
        public override EnumStates GetState() { return STATE; }

        private State previousState;

        public StuckState(InternAI ai)
            : base(ai)
        {
            this.previousState = ai.State;
            this.EnumStuckStates = this.previousState.EnumStuckStates;
        }

        public override void DoAI()
        {
            if (!ai.IsStuck)
            {
                ChangeToPreviousState();
            }

            if (ai.TimeSinceStuck > Const.TIMER_STUCK_TOO_MUCH)
            {
                Plugin.Logger.LogDebug($"- Stuck since too much - ({Const.TIMER_STUCK_TOO_MUCH}sec) -> teleport");
                // Teleport player
                npcPilot.Npc.thisPlayerBody.transform.position = ai.transform.position;
                ChangeToPreviousState();
                return;
            }

            InteractTrigger? ladder = ai.GetLadderIfWantsToUseLadder();
            if (ladder != null)
            {
                Plugin.Logger.LogDebug("-> wants to use ladder");
                ladder.Interact(npcPilot.Npc.thisPlayerBody);
                ChangeToPreviousState();
                return;
            }

            switch (EnumStuckStates)
            {
                case EnumStuckStates.TryToJump:
                    Plugin.Logger.LogDebug("Jump ?");
                    npcPilot.OrderToJump();
                    ChangeStuckStateTo(EnumStuckStates.TryToCrouch);
                    break;
                case EnumStuckStates.TryToCrouch:
                    Plugin.Logger.LogDebug("Crouch ?");
                    npcPilot.OrderToToggleCrouch();
                    ChangeStuckStateTo(EnumStuckStates.TryToJump);// Loop stuck states
                    break;
            }
        }

        private void ChangeStuckStateTo(EnumStuckStates stuckState)
        {
            EnumStuckStates = stuckState;
            this.previousState.EnumStuckStates = EnumStuckStates;
        }

        private void ChangeToPreviousState()
        {
            ai.TimeSinceStuck = 0f;
            ai.State = previousState;
            Plugin.Logger.LogDebug($"new state :                 {previousState.GetState()}");
            ai.SwitchToBehaviourState((int)previousState.GetState());
        }
    }
}
