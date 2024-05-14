using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class ChillWithPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.ChillWithPlayer;
        public override EnumStates GetState() { return STATE; }
        
        private PlayerControllerB? player;
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

        public ChillWithPlayerState(State state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                || sqrVerticalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // todo check sound
                npcPilot.OrderToLookForward();

                player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (player != null && ai.PlayerIsTargetable(player))
                {
                    // Target still here but too far
                    targetLastKnownPosition = player.transform.position;
                    npcPilot.SetTurnBodyTowardsDirection(targetLastKnownPosition.Value);
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }
                else
                {
                    // Target lost
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
            }

            player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null && ai.PlayerIsTargetable(player))
            {
                // Target still close
                targetLastKnownPosition = player.transform.position;
            }

            // Looking
            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                npcPilot.OrderToLookAtPlayer(player);
            }
            else
            {
                npcPilot.OrderToLookForward();
            }

            // Chill
            ai.StopMoving();
        }
    }
}
