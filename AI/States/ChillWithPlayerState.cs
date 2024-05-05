namespace NWTWA.AI.States
{
    internal class ChillWithPlayerState : State
    {
        public ChillWithPlayerState(State state) : base(state) { }

        public override void DoAI()
        {
            //agent.velocity = Vector3.zero;
            //this.NpcControllerB.Npc.thisController.transform.position = base.transform.position;
            ai.agent.ResetPath();
            npcPilot.StopMoving();
            ai.transform.position = npcPilot.Npc.thisController.transform.position;
        }
    }
}
