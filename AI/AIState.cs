using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI
{
    /// <summary>
    /// Abstract state main class for the <c>AIState</c>
    /// </summary>
    internal abstract class AIState
    {
        protected InternAI ai;

        private EnumAIStates currentState;
        protected EnumAIStates CurrentState {
            get
            {
                return this.currentState;
            }
            set
            {
                this.currentState = value;
                Plugin.LogDebug($"Intern {npcController.Npc.playerClientId} ({npcController.Npc.playerUsername}) new state :                 {this.currentState}");
            }
        }

        /// <summary>
        /// <c>NpcController</c> from the <c>InternAI</c>
        /// </summary>
        protected NpcController npcController;
        protected AISearchRoutine searchForPlayers;

        protected Vector3? targetLastKnownPosition;
        protected GrabbableObject? targetItem;

        protected Coroutine? panikCoroutine;
        protected Transform? enemyTransform;

        /// <summary>
        /// Constructor from another state
        /// </summary>
        /// <param name="oldState"></param>
        protected AIState(AIState oldState) : this(oldState.ai)
        {
            this.targetLastKnownPosition = oldState.targetLastKnownPosition;
            this.targetItem = oldState.targetItem;

            this.panikCoroutine = oldState.panikCoroutine;
            this.enemyTransform = oldState.enemyTransform;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ai"></param>
        /// <exception cref="System.NullReferenceException"><c>InternAI</c> null in parameters</exception>
        protected AIState(InternAI ai)
        {
            if (ai == null)
            {
                throw new System.NullReferenceException("Enemy AI is null.");
            }

            this.ai = ai;

            this.npcController = ai.NpcController;

            this.searchForPlayers = new AISearchRoutine();
            this.searchForPlayers.randomized = true;
        }

        /// <summary>
        /// Apply the behaviour according to the type of state <see cref="Enums.EnumAIStates"><c>Enums.EnumAIStates</c></see>.<br/>
        /// </summary>
        public abstract void DoAI();

        public virtual void PlayerHeard(Vector3 noisePosition) { }

        /// <summary>
        /// Get the <see cref="Enums.EnumAIStates"><c>Enums.EnumAIStates</c></see> of current State
        /// </summary>
        /// <returns></returns>
        public virtual EnumAIStates GetAIState() { return CurrentState; }

        public virtual string GetBillboardStateIndicator() { return string.Empty; }
    }
}
