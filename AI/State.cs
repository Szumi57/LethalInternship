using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI
{
    internal abstract class State
    {
        protected InternAI ai;
        protected NpcController npcController;
        protected AISearchRoutine searchForPlayers;

        protected Vector3? targetLastKnownPosition;
        protected GrabbableObject? targetItem;
        
        public float TimeAtLastUsingEntrance { get; set; }

        protected State(State newState) : this(newState.ai)
        {
            this.targetLastKnownPosition = newState.targetLastKnownPosition;
            this.targetItem = newState.targetItem;
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

            this.npcController = ai.NpcController;

            this.searchForPlayers = new AISearchRoutine();
            this.searchForPlayers.randomized = true;
        }

        public abstract void DoAI();

        public abstract EnumStates GetState();
    }
}
