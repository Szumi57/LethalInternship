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
                Plugin.Logger.LogDebug($"{ai.NpcController.Npc.playerUsername} Looking around to find player {lookingAroundTimer}");
                ai.StopMoving();
                return;
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

            // Try to reach target last known position
            if (!targetLastKnownPosition.HasValue)
            {
                ai.State = new SearchingForPlayerState(this);
                return;
            }
            ai.SetDestinationToPositionInternAI(targetLastKnownPosition.Value);

            if (SqrDistanceWithTargetLastKnownPosition < Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION * Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION)
            {
                npcController.OrderToStopSprint();
            }
            else
            {
                npcController.OrderToSprint();
            }

            Plugin.Logger.LogDebug($"{ai.NpcController.Npc.playerUsername} distance to last position {Vector3.Distance(targetLastKnownPosition.Value, npcController.Npc.transform.position)}");
            if (SqrDistanceWithTargetLastKnownPosition < Const.DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION * Const.DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION)
            {
                // Check for teleport entrance
                if (Time.timeSinceLevelLoad - TimeSinceUsingEntrance > Const.WAIT_TIME_TO_TELEPORT)
                {
                    EntranceTeleport? entrance = ai.IsEntranceCloseForBoth(targetLastKnownPosition.Value, npcController.Npc.transform.position);
                    if (entrance != null)
                    {
                        Vector3? entranceTeleportPos = ai.GetTeleportPosOfEntrance(entrance);
                        if (entranceTeleportPos.HasValue)
                        {
                            targetLastKnownPosition = ai.targetPlayer.transform.position;
                            Plugin.Logger.LogDebug($"======== TeleportInternAndSync {ai.NpcController.Npc.playerUsername} !!!!!!!!!!!!!!! ");
                            ai.TeleportInternAndSync(entranceTeleportPos.Value, !ai.isOutside, true);
                            ai.SetDestinationToPositionInternAI(targetLastKnownPosition.Value);
                            return;
                        }
                    }

                    PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    if (player == null)
                    {
                        player = ai.CheckLOSForInternHavingTargetInLOS(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    }
                    if (player != null)
                    {
                        // Target found
                        targetLastKnownPosition = player.transform.position;
                        ai.State = new GetCloseToPlayerState(this);
                        return;
                    }

                    // Start looking around
                    lookingAroundTimer += ai.AIIntervalTime;
                    return;
                }
            }

            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target != null)
            {
                // Target found
                targetLastKnownPosition = target.transform.position;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            ai.OrderMoveToDestination();
        }
    }
}
