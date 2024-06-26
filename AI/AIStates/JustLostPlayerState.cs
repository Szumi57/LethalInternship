﻿using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class JustLostPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.JustLostPlayer;
        public override EnumAIStates GetAIState() { return STATE; }

        private float lookingAroundTimer;
        private float SqrDistanceToTargetLastKnownPosition
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

        public JustLostPlayerState(AIState state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

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
            if (ai.AreHandsFree())
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

            Plugin.Logger.LogDebug($"{npcController.Npc.playerUsername} distance to last position {Vector3.Distance(targetLastKnownPosition.Value, npcController.Npc.transform.position)}");
            if (SqrDistanceToTargetLastKnownPosition < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                // Check for teleport entrance
                if (Time.timeSinceLevelLoad - ai.TimeSinceUsingEntrance > Const.WAIT_TIME_TO_TELEPORT)
                {
                    EntranceTeleport? entrance = ai.IsEntranceCloseForBoth(targetLastKnownPosition.Value, npcController.Npc.transform.position);
                    Vector3? entranceTeleportPos = ai.GetTeleportPosOfEntrance(entrance);
                    if (entranceTeleportPos.HasValue)
                    {
                        Plugin.Logger.LogDebug($"======== TeleportInternAndSync {ai.NpcController.Npc.playerUsername} !!!!!!!!!!!!!!! ");
                        ai.TeleportInternAndSync(entranceTeleportPos.Value, !ai.isOutside, true);
                        targetLastKnownPosition = ai.targetPlayer.transform.position;
                    }
                    else
                    {
                        // Start looking around
                        lookingAroundTimer += ai.AIIntervalTime;
                        return;
                    }
                }
            }

            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target != null)
            {
                // Target found
                targetLastKnownPosition = target.transform.position;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            ai.SetDestinationToPositionInternAI(targetLastKnownPosition.Value);

            if (SqrDistanceToTargetLastKnownPosition < Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION * Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION)
            {
                npcController.OrderToStopSprint();
            }
            else
            {
                npcController.OrderToSprint();
            }

            ai.OrderMoveToDestination();
        }
    }
}
