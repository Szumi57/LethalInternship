using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.Interns.AI.Commands
{
    public class LostPlayerCommand : ICommandAI
    {
        private readonly InternAI ai;
        private NpcController Controller { get { return ai.NpcController; } }
        private Vector3? TargetLastKnownPosition { get { return ai.TargetLastKnownPosition; } set { ai.TargetLastKnownPosition = value; } }

        private float LookingAroundTimer { get { return ai.LookingAroundTimer; } set { ai.LookingAroundTimer = value; } }
        private Coroutine LookingAroundCoroutine { get { return ai.LookingAroundCoroutine; } set { ai.LookingAroundCoroutine = value; } }

        public LostPlayerCommand(InternAI ai)
        {
            this.ai = ai;
        }

        public void Execute()
        {
            // Looking around for too long, stop the coroutine, the target player is officially lost
            if (LookingAroundTimer > Const.TIMER_LOOKING_AROUND)
            {
                LookingAroundTimer = 0f;
                TargetLastKnownPosition = null;

                StopLookingAroundCoroutine();
            }

            // If the looking around timer is started
            // Start of the coroutine for making the intern looking around him
            if (LookingAroundTimer > 0f)
            {
                LookingAroundTimer += ai.AIIntervalTime;
                Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} Looking around to find player {LookingAroundTimer}");
                ai.StopMoving();

                StartLookingAroundCoroutine();

                if (CheckLOSForTargetAndGetClose())
                {
                    ai.QueueNewCommand(new FollowPlayerCommand(ai));
                    return;
                }

                ai.QueueNewCommand(this);
                return;
            }

            // Try to reach target last known position
            if (!TargetLastKnownPosition.HasValue)
            {
                ai.QueueNewCommand(new LookingForPlayerCommand(ai));
                return;
            }

            Plugin.LogDebug($"{Controller.Npc.playerUsername} distance to last position {Vector3.Distance(TargetLastKnownPosition.Value, Controller.Npc.transform.position)}");
            // If the intern is close enough to the last known position
            float sqrDistanceToTargetLastKnownPosition = (TargetLastKnownPosition.Value - Controller.Npc.transform.position).sqrMagnitude;
            if (sqrDistanceToTargetLastKnownPosition < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                // Check for teleport entrance
                if (Time.timeSinceLevelLoad - ai.TimeSinceTeleporting > Const.WAIT_TIME_TO_TELEPORT)
                {
                    EntranceTeleport? entrance = ai.IsEntranceCloseForBoth(TargetLastKnownPosition.Value, Controller.Npc.transform.position);
                    Vector3? entranceTeleportPos = ai.GetTeleportPosOfEntrance(entrance);
                    if (entranceTeleportPos.HasValue)
                    {
                        Plugin.LogDebug($"======== TeleportInternAndSync {Controller.Npc.playerUsername} !!!!!!!!!!!!!!! ");
                        ai.SyncTeleportIntern(entranceTeleportPos.Value, !ai.isOutside, true);
                        TargetLastKnownPosition = ai.targetPlayer.transform.position;
                    }
                    else
                    {
                        // Start looking around
                        if (LookingAroundTimer == 0f)
                        {
                            LookingAroundTimer += ai.AIIntervalTime;
                        }

                        ai.QueueNewCommand(this);
                        return;
                    }
                }
            }

            // Check if we see the target player
            // Or a new target player if target player is null
            if (CheckLOSForTargetOrClosestPlayer())
            {
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return;
            }

            // Go to the last known position
            ai.SetDestinationToPositionInternAI(TargetLastKnownPosition.Value);

            // Sprint if too far, unsprint if close enough
            if (sqrDistanceToTargetLastKnownPosition < Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION * Const.DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION)
            {
                Controller.OrderToStopSprint();
            }
            else
            {
                Controller.OrderToSprint();
            }

            ai.NpcController.OrderToMove();
            // Destination after path checking might be not the same now
            TargetLastKnownPosition = ai.destination;

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return;
        }

        public string GetBillboardStateIndicator()
        {
            return "!?";
        }

        public void PlayerHeard(Vector3 noisePosition)
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.HearsPlayer,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        private void TryPlayCurrentStateVoiceAudio()
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.LosingPlayer,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        /// <summary>
        /// Check if the target player is in line of sight
        /// </summary>
        private bool CheckLOSForTargetAndGetClose()
        {
            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target == null)
            {
                return false;
            }

            // Target found
            StopLookingAroundCoroutine();
            TargetLastKnownPosition = target.transform.position;

            // Voice
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.LostAndFound,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });

            return true;
        }

        private bool CheckLOSForTargetOrClosestPlayer()
        {
            if (ai.targetPlayer != null)
            {
                return CheckLOSForTargetAndGetClose();
            }

            PlayerControllerB? newTarget = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (newTarget != null)
            {
                // new target
                ai.SyncAssignTargetAndSetMovingTo(newTarget);
                if (Plugin.Config.ChangeSuitAutoBehaviour.Value)
                {
                    ai.ChangeSuitInternServerRpc(Controller.Npc.playerClientId, newTarget.currentSuitID);
                    return true;
                }
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
            }

            return false;
        }

        /// <summary>
        /// Coroutine for making intern turn his body to look around him
        /// </summary>
        /// <returns></returns>
        private IEnumerator LookingAround()
        {
            yield return null;
            while (LookingAroundTimer < Const.TIMER_LOOKING_AROUND)
            {
                float freezeTimeRandom = Random.Range(Const.MIN_TIME_FREEZE_LOOKING_AROUND, Const.MAX_TIME_FREEZE_LOOKING_AROUND);
                float angleRandom = Random.Range(-180, 180);
                Controller.SetTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * Controller.Npc.thisController.transform.forward);
                yield return new WaitForSeconds(freezeTimeRandom);
            }
        }

        private void StartLookingAroundCoroutine()
        {
            if (LookingAroundCoroutine == null)
            {
                LookingAroundCoroutine = ai.StartCoroutine(LookingAround());
            }
        }

        private void StopLookingAroundCoroutine()
        {
            if (LookingAroundCoroutine != null)
            {
                ai.StopCoroutine(LookingAroundCoroutine);
            }
        }

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.None;
        }
    }
}
