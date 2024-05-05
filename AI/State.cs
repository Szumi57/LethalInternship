using GameNetcodeStuff;

namespace NWTWA.AI
{
    internal abstract class State
    {
        protected InternAI ai;
        protected NpcPilot npcPilot;

        protected AISearchRoutine searchForPlayers;
        protected PlayerControllerB? playerControllerB;

        public float TimeAtLastUsingEntrance { get; set; }


        protected State(State state) : this(state.ai)
        {
        }

        protected State(InternAI ai)
        {
            if (ai == null)
            {
                throw new System.Exception("Enemy AI is null.");
            }
            Plugin.Logger.LogDebug($"new state : {(Enums.EnumStates)ai.currentBehaviourStateIndex}");

            this.ai = ai;
            this.npcPilot = ai.NpcPilot;

            this.searchForPlayers = new AISearchRoutine();
            this.searchForPlayers.randomized = true;
        }

        public abstract void DoAI();
    }
}
