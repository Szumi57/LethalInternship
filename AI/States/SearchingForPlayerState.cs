using NWTWA.Enums;
using UnityEngine;

namespace NWTWA.AI.States
{
    internal class SearchingForPlayerState : State
    {
        public SearchingForPlayerState(State state) : base(state) { }
        public SearchingForPlayerState(InternAI ai) : base(ai) { }

        public override void DoAI()
        {
            if (Time.realtimeSinceStartup - TimeAtLastUsingEntrance > 3f
                        && !ai.GetClosestPlayer(false, false, false)
                        && !ai.PathIsIntersectedByLineOfSight(ai.mainEntrancePosition, false, false))
            {
                if (Vector3.Distance(ai.transform.position, ai.mainEntrancePosition) < 1f)
                {
                    ai.TeleportInternAndSync(RoundManager.FindMainEntrancePosition(true, !ai.isOutside), !ai.isOutside);
                    return;
                }
                if (searchForPlayers.inProgress)
                {
                    ai.StopSearch(searchForPlayers, true);
                }
                ai.SetDestinationToPosition(ai.mainEntrancePosition, false);
                return;
            }
            else
            {
                if (!searchForPlayers.inProgress)
                {
                    ai.StartSearch(ai.transform.position, searchForPlayers);
                }
                playerControllerB = ai.CheckLineOfSightForClosestPlayer(50f, 60, -1, 0f);
                if (playerControllerB != null)
                {
                    ai.SetMovingTowardsTargetPlayer(playerControllerB);
                    ChangeStateToGetCloseToPlayer();
                    return;
                }
            }

            ai.agent.SetDestination(ai.destination);
            npcPilot.SetTargetPosition(ai.transform.position);
            ai.agent.nextPosition = npcPilot.Npc.thisController.transform.position;
        }

        private void ChangeStateToGetCloseToPlayer()
        {
            ai.SwitchToBehaviourState((int)EnumStates.GetCloseToPlayer);
            ai.State = new GetCloseToPlayerState(this);
        }
    }
}
