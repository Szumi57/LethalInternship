using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches;
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
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
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
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (ai.PlayerIsTargetable(ai.targetPlayer))
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.SetMovingTowardsTargetPlayer(ai.targetPlayer);
                ai.destination = RoundManager.Instance.GetNavMeshPosition(ai.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                //Plugin.Logger.LogDebug($"setdestination");
            }
            else
            {
                // Target is not available anymore
                ai.State = new SearchingForPlayerState(this);
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
            if (PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch(npcController.Npc) > -1)
            {
                GameObject gameObjectGrabbleObject = ai.CheckLineOfSightForObjects();
                if (gameObjectGrabbleObject)
                {
                    GrabbableObject component = gameObjectGrabbleObject.GetComponent<GrabbableObject>();
                    if (component && !component.isHeld)
                    {
                        ai.SetDestinationToPositionInternAI(gameObjectGrabbleObject.transform.position);
                        this.targetItem = component;
                        ai.State = new FetchingObjectState(this);
                        return;
                    }
                }
            }

            PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player == null)
            {
                Plugin.Logger.LogDebug($"no see player, but still in range, too far {SqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {SqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                if(SqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    || SqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
            }

            //Plugin.Logger.LogDebug($"OrderMoveToDestination");
            ai.OrderMoveToDestination();
        }
    }
}
