using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class GetCloseToPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.GetCloseToPlayer;
        public override EnumStates GetState() { return STATE; }

        private float sqrHorizontalDistanceWithTarget
        {
            get
            {
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
                return Vector3.Scale((ai.targetPlayer.transform.position - npcPilot.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        private float sqrVerticalDistanceWithTarget
        {
            get
            {
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
                return Vector3.Scale((ai.targetPlayer.transform.position - npcPilot.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
            }
        }

        public GetCloseToPlayerState(State state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            if (ai.targetPlayer == null)
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (ai.PlayerIsTargetable(ai.targetPlayer))
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.SetMovingTowardsTargetPlayer(ai.targetPlayer);
                ai.destination = RoundManager.Instance.GetNavMeshPosition(ai.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
            }
            else
            {
                // Target is not available anymore
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            //Plugin.Logger.LogDebug($"sqrHorizontalDistanceWithTarget {sqrHorizontalDistanceWithTarget}, sqrVerticalDistanceWithTarget {sqrVerticalDistanceWithTarget}");
            // Follow player
            if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
            {
                npcPilot.OrderToSprint();
                // todo rpc
                //    SetRunningServerRpc(true);
            }
            else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                     && sqrVerticalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.State = new ChillWithPlayerState(this);
                return;
            }
            else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
            {
                npcPilot.OrderToStopSprint();
                // todo rpc
                //    SetRunningServerRpc(false);
            }

            PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player == null)
            {
                Plugin.Logger.LogDebug($"no see player, but still in range, too far {sqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {sqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                if(sqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    || sqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
            }

            ai.OrderMoveToDestination();
        }
    }
}
