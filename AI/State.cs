using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI
{
    internal abstract class State
    {
        protected InternAI ai;
        protected NpcController npcPilot;
        protected AISearchRoutine searchForPlayers;

        protected Vector3? targetLastKnownPosition;
        
        public EnumStuckStates EnumStuckStates;
        public float TimeAtLastUsingEntrance { get; set; }

        protected State(State newState) : this(newState.ai)
        {
            this.targetLastKnownPosition = newState.targetLastKnownPosition;
        }

        protected State(InternAI ai)
        {
            if (ai == null)
            {
                throw new System.Exception("Enemy AI is null.");
            }
            Plugin.Logger.LogDebug($"new state :                 {this.GetState()}");

            this.ai = ai;
            this.ai.SwitchToBehaviourState((int)this.GetState());

            this.npcPilot = ai.NpcController;

            this.searchForPlayers = new AISearchRoutine();
            this.searchForPlayers.randomized = true;
        }

        public abstract void DoAI();

        public abstract EnumStates GetState();
    }
}
