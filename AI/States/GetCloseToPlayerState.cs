using NWTWA.Enums;

namespace NWTWA.AI.States
{
    internal class GetCloseToPlayerState : State
    {
        private bool lostPlayerInChase;
        private float lostLOSTimer;

        public GetCloseToPlayerState(State state) : base(state) { }

        public override void DoAI()
        {
            //this.LookAndRunRandomly(true, true);
            playerControllerB = ai.CheckLineOfSightForClosestPlayer(70f, 50, 1, 3f);
            if (playerControllerB != null)
            {
                lostPlayerInChase = false;
                lostLOSTimer = 0f;
                if (playerControllerB != ai.targetPlayer)
                {
                    ai.SetMovingTowardsTargetPlayer(playerControllerB);
                    //this.LookAtPlayerServerRpc((int)this.PlayerControllerB.playerClientId);
                }
                if (ai.mostOptimalDistance > 12f)
                {
                    //if (!running)
                    //{
                    //    running = true;
                    //    creatureAnimator.SetBool("Sprinting", true);
                    //    Plugin.Logger.LogDebug(string.Format("Setting running to true 8; {0}", creatureAnimator.GetBool("Running")));
                    //    SetRunningServerRpc(true);
                    //}
                }
                else if (ai.mostOptimalDistance < 4f)
                {
                    // Stop movement
                    ChangeStateToChillWithPlayer();
                    return;
                }
                else if (ai.mostOptimalDistance < 8f)
                {
                    //if (running && !runningRandomly)
                    //{
                    //    running = false;
                    //    creatureAnimator.SetBool("Sprinting", false);
                    //    Plugin.Logger.LogDebug(string.Format("Setting running to false 1; {0}", creatureAnimator.GetBool("Running")));
                    //    SetRunningServerRpc(false);
                    //}
                }
            }
            else
            {
                lostLOSTimer += ai.AIIntervalTime;
                if (lostLOSTimer > 10f)
                {
                    ai.targetPlayer = null;
                    ChangeStateToSearchingForPlayer();
                    return;
                }
                else if (lostLOSTimer > 3.5f)
                {
                    lostPlayerInChase = true;
                    //StopLookingAtTransformServerRpc();
                    ai.targetPlayer = null;
                    //if (running)
                    //{
                    //    running = false;
                    //    creatureAnimator.SetBool("Sprinting", false);
                    //    Plugin.Logger.LogDebug(string.Format("Setting running to false 2; {0}", creatureAnimator.GetBool("Running")));
                    //    SetRunningServerRpc(false);
                    //}
                }
            }

            if (ai.targetPlayer != null
                && ai.PlayerIsTargetable(ai.targetPlayer, false, false))
            {
                if (lostPlayerInChase)
                {
                    if (!searchForPlayers.inProgress)
                    {
                        ai.StartSearch(ai.transform.position, searchForPlayers);
                        return;
                    }
                }
                else
                {
                    if (searchForPlayers.inProgress)
                    {
                        ai.StopSearch(searchForPlayers, true);
                    }
                    ai.SetMovingTowardsTargetPlayer(ai.targetPlayer);
                }
            }

            ai.agent.SetDestination(ai.destination);
            npcPilot.SetTargetPosition(ai.transform.position);
            ai.agent.nextPosition = npcPilot.Npc.thisController.transform.position;
        }

        private void ChangeStateToSearchingForPlayer()
        {
            ai.SwitchToBehaviourState((int)EnumStates.SearchingForPlayer);
            ai.State = new SearchingForPlayerState(this);
        }

        private void ChangeStateToChillWithPlayer()
        {
            ai.SwitchToBehaviourState((int)EnumStates.ChillWithPlayer);
            ai.State = new ChillWithPlayerState(this);
        }
    }
}
