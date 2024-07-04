using GameNetcodeStuff;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class JustLostPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.JustLostPlayer;
        public override EnumAIStates GetAIState() { return STATE; }

        private float lookingAroundTimer;
        private Coroutine lookingAroundCoroutine = null!;

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

                StopLookingAroundCoroutine();
            }

            if (lookingAroundTimer > 0f)
            {
                lookingAroundTimer += ai.AIIntervalTime;
                Plugin.Logger.LogDebug($"{ai.NpcController.Npc.playerUsername} Looking around to find player {lookingAroundTimer}");
                ai.StopMoving();

                StartLookingAroundCoroutine();

                CheckLOSForTarget();

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
                        if (lookingAroundTimer == 0f)
                        {
                            lookingAroundTimer += ai.AIIntervalTime;
                        }

                        return;
                    }
                }
            }

            CheckLOSForTarget();

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

        private void CheckLOSForTarget()
        {
            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target != null)
            {
                // Target found
                StopLookingAroundCoroutine();
                targetLastKnownPosition = target.transform.position;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }
        }

        private IEnumerator LookingAround()
        {
            yield return null;
            while (lookingAroundTimer < Const.TIMER_LOOKING_AROUND)
            {
                float freezeTimeRandom = Random.Range(Const.MIN_TIME_FREEZE_LOOKING_AROUND, Const.MAX_TIME_FREEZE_LOOKING_AROUND);
                float angleRandom = Random.Range(-180, 180);
                npcController.SetTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * npcController.Npc.thisController.transform.forward);
                yield return new WaitForSeconds(freezeTimeRandom);
            }
        }

        private void StartLookingAroundCoroutine()
        {
            if (this.lookingAroundCoroutine == null)
            {
                this.lookingAroundCoroutine = ai.StartCoroutine(this.LookingAround());
            }
        }

        private void StopLookingAroundCoroutine()
        {
            if (this.lookingAroundCoroutine != null)
            {
                ai.StopCoroutine(this.lookingAroundCoroutine);
            }
        }
    }
}
