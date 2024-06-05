using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI
{
    internal abstract class AIState
    {
        protected InternAI ai;
        protected NpcController npcController;
        protected AISearchRoutine searchForPlayers;

        protected Vector3? targetLastKnownPosition;
        protected GrabbableObject? targetItem;
        
        public float TimeSinceUsingEntrance { get; set; }

        protected AIState(AIState newState) : this(newState.ai)
        {
            this.targetLastKnownPosition = newState.targetLastKnownPosition;
            this.targetItem = newState.targetItem;
            this.TimeSinceUsingEntrance = newState.TimeSinceUsingEntrance;
        }

        protected AIState(InternAI ai)
        {
            if (ai == null)
            {
                throw new System.Exception("Enemy AI is null.");
            }

            this.ai = ai;
            this.ai.SwitchToBehaviourState((int)this.GetAIState());

            this.npcController = ai.NpcController;
            Plugin.Logger.LogDebug($"Intern {npcController.Npc.playerClientId} new state :                 {this.GetAIState()}");

            this.searchForPlayers = new AISearchRoutine();
            this.searchForPlayers.randomized = true;
        }

        public abstract void DoAI();

        public abstract EnumAIStates GetAIState();
    }
}
