using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the intern cannot see the target player and try to reach his last known (seen) position
    /// </summary>
    public class JustLostPlayerState : AIState
    {
        private float lookingAroundTimer;
        private Coroutine lookingAroundCoroutine = null!;

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public JustLostPlayerState(AIState state) : base(state)
        {
            CurrentState = EnumAIStates.JustLostPlayer;

            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // Looking around for too long, stop the coroutine, the target player is officially lost
            if (lookingAroundTimer > Const.TIMER_LOOKING_AROUND)
            {
                lookingAroundTimer = 0f;
                targetLastKnownPosition = null;

                StopLookingAroundCoroutine();
            }

            // If the looking around timer is started
            // Start of the coroutine for making the intern looking around him
            if (lookingAroundTimer > 0f)
            {
                lookingAroundTimer += ai.AIIntervalTime;
                Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} Looking around to find player {lookingAroundTimer}");
                ai.StopMoving();

                StartLookingAroundCoroutine();

                CheckLOSForTargetAndGetClose();

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

            Plugin.LogDebug($"{npcController.Npc.playerUsername} distance to last position {Vector3.Distance(targetLastKnownPosition.Value, npcController.Npc.transform.position)}");
            // If the intern is close enough to the last known position
            float sqrDistanceToTargetLastKnownPosition = (targetLastKnownPosition.Value - npcController.Npc.transform.position).sqrMagnitude;
            if (sqrDistanceToTargetLastKnownPosition < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                // Check for teleport entrance
                if (Time.timeSinceLevelLoad - ai.TimeSinceTeleporting > Const.WAIT_TIME_TO_TELEPORT)
                {
                    EntranceTeleport? entrance = ai.IsEntranceCloseForBoth(targetLastKnownPosition.Value, npcController.Npc.transform.position);
                    Vector3? entranceTeleportPos = ai.GetTeleportPosOfEntrance(entrance);
                    if (entranceTeleportPos.HasValue)
                    {
                        Plugin.LogDebug($"======== TeleportInternAndSync {ai.NpcController.Npc.playerUsername} !!!!!!!!!!!!!!! ");
                        ai.SyncTeleportIntern(entranceTeleportPos.Value, !ai.isOutside, true);
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

            // Check if we see the target player
            // Or a new target player if target player is null
            CheckLOSForTargetOrClosestPlayer();

            // Go to the last known position
            ai.SetDestinationToPositionInternAI(targetLastKnownPosition.Value);

            // Sprint if too far, unsprint if close enough
            if (sqrDistanceToTargetLastKnownPosition < Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION * Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION)
            {
                npcController.OrderToStopSprint();
            }
            else
            {
                npcController.OrderToSprint();
            }

            ai.OrderMoveToDestination();
            // Destination after path checking might be not the same now
            targetLastKnownPosition = ai.destination;
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.LosingPlayer,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = npcController.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public override void PlayerHeard(Vector3 noisePosition)
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.HearsPlayer,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = npcController.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public override string GetBillboardStateIndicator()
        {
            return "!?";
        }

        /// <summary>
        /// Check if the target player is in line of sight
        /// </summary>
        private void CheckLOSForTargetAndGetClose()
        {
            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target != null)
            {
                // Target found
                StopLookingAroundCoroutine();
                targetLastKnownPosition = target.transform.position;

                // Voice
                ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                {
                    VoiceState = EnumVoicesState.LostAndFound,
                    CanTalkIfOtherInternTalk = false,
                    WaitForCooldown = true,
                    CutCurrentVoiceStateToTalk = false,

                    ShouldSync = true,
                    IsInternInside = npcController.Npc.isInsideFactory,
                    AllowSwearing = Plugin.Config.AllowSwearing.Value
                });

                ai.State = new GetCloseToPlayerState(this);
                return;
            }
        }

        private void CheckLOSForTargetOrClosestPlayer()
        {
            if (ai.targetPlayer == null)
            {
                PlayerControllerB? newTarget = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (newTarget != null)
                {
                    // new target
                    ai.SyncAssignTargetAndSetMovingTo(newTarget);
                    if (Plugin.Config.ChangeSuitAutoBehaviour.Value)
                    {
                        ai.ChangeSuitInternServerRpc(npcController.Npc.playerClientId, newTarget.currentSuitID);
                    }
                }
            }
            else
            {
                CheckLOSForTargetAndGetClose();
            }
        }

        /// <summary>
        /// Coroutine for making intern turn his body to look around him
        /// </summary>
        /// <returns></returns>
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
