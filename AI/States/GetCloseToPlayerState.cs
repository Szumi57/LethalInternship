using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class GetCloseToPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.GetCloseToPlayer;
        public override EnumStates GetState() { return STATE; }

        private float SqrHorizontalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
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
                if (this.targetLastKnownPosition.HasValue)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }

                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (!ai.PlayerIsTargetable(ai.targetPlayer, false, true))
            {
                // Target is not available anymore
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            // Target in ship, wait outside
            if (ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new PlayerInShipState(this);
                return;
            }

            // Follow player
            //Plugin.Logger.LogDebug($"sqrHorizontalDistanceWithTarget {sqrHorizontalDistanceWithTarget}, sqrVerticalDistanceWithTarget {sqrVerticalDistanceWithTarget}");
            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                || SqrVerticalDistanceWithTarget > 0.3f * 0.3f)
            {
                npcController.OrderToSprint();
                // todo rpc
                //    SetRunningServerRpc(true);
            }
            else if (SqrHorizontalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                     && SqrVerticalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.State = new ChillWithPlayerState(this);
                return;
            }
            else if (SqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
            {
                npcController.OrderToStopSprint();
                // todo rpc
                //    SetRunningServerRpc(false);
            }

            // Check for object to grab
            if (ai.HandsFree())
            {
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    ai.State = new FetchingObjectState(this, grabbableObject);
                    return;
                }
            }

            PlayerControllerB? checkTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (checkTarget == null)
            {
                if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    || SqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
                {
                    Plugin.Logger.LogDebug($"no see target, still in range ? too far {SqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {SqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
            }
            else
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.AssignTargetAndSetMovingTo(ai.targetPlayer);
            }

            ai.OrderMoveToDestination();
        }
    }
}
