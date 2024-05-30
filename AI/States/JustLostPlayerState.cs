using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class JustLostPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.JustLostPlayer;
        public override EnumStates GetState() { return STATE; }

        private PlayerControllerB? player;
        private float lookingAroundTimer;
        private float SqrDistanceWithTargetLastKnownPosition
        {
            get
            {
                if (!targetLastKnownPosition.HasValue)
                {
                    return 0f;
                }

                return (targetLastKnownPosition.Value - npcController.Npc.transform.position).sqrMagnitude;
                //return (targetLastKnownPosition.Value - ai.transform.position).sqrMagnitude;
            }
        }

        public JustLostPlayerState(State state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            if (lookingAroundTimer > Const.TIMER_LOOKING_AROUND)
            {
                lookingAroundTimer = 0f;
                targetLastKnownPosition = null;
            }

            if (lookingAroundTimer > 0f)
            {
                // todo Look around randomly ?
                lookingAroundTimer += ai.AIIntervalTime;
                Plugin.Logger.LogDebug($"Looking around to find player {lookingAroundTimer}");
                ai.StopMoving();
                return;
            }

            // Check for object to grab
            if (PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch(npcController.Npc) > -1)
            {
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    ai.SetDestinationToPositionInternAI(grabbableObject.transform.position);
                    this.targetItem = grabbableObject;
                    ai.State = new FetchingObjectState(this);
                    return;
                }
            }

            // Try to reach target last known position
            if (!targetLastKnownPosition.HasValue)
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (Time.realtimeSinceStartup - TimeSinceUsingEntrance > Const.WAIT_TIME_TO_TELEPORT)
            {
                EntranceTeleport? entrance = ai.IsEntranceCloseForBoth(targetLastKnownPosition.Value, npcController.Npc.transform.position);
                if (entrance != null)
                {
                    Vector3? entranceTeleportPos = ai.GetTeleportPosOfEntrance(entrance);
                    if (entranceTeleportPos.HasValue)
                    {
                        targetLastKnownPosition = null;
                        ai.State = new GetCloseToPlayerState(this);
                        Plugin.Logger.LogDebug($"======== TeleportInternAndSync !!!!!!!!!!!!!!! ");
                        ai.TeleportInternAndSync(entranceTeleportPos.Value, !ai.isOutside, true);
                        return;
                    }
                }
            }

            Plugin.Logger.LogDebug($"distance to last position {Vector3.Distance(targetLastKnownPosition.Value, ai.transform.position)}");
            //npcController.OrderToLookForward();
            if (SqrDistanceWithTargetLastKnownPosition < Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION * Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION)
            {
                npcController.OrderToStopSprint();
            }
            else
            {
                npcController.OrderToSprint();
            }

            if ((ai.transform.position - targetLastKnownPosition.Value).sqrMagnitude < Const.DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION * Const.DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION)
            {
                lookingAroundTimer += ai.AIIntervalTime;
            }
            else
            {
                ai.SetDestinationToPositionInternAI(targetLastKnownPosition.Value);
            }

            player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                // Target found
                targetLastKnownPosition = player.transform.position;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            ai.OrderMoveToDestination();
        }
    }
}
