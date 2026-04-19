using GameNetcodeStuff;
using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class Chill : IBTAction
    {
        private InternAI ai = null!;

        private Vector2 idlePauseRange = new Vector2(1.5f, 4f);
        private Vector2 lookDurationRange = new Vector2(0.8f, 2.5f);

        private float randomLookAngle = 70f;
        private float randomLookDistance = 8f;

        private Transform currentLookTarget = null!;
        private float maxLookDistance = 10f;

        private float timerLookingAtTarget;
        private float lastTime = -1f;

        public BehaviourTreeStatus Action(BTContext context)
        {
            this.ai = context.InternAI;

            context.ChillCoroutine.StartCoroutine(ChillLookRoutine());
            context.ChillCoroutine.KeepAlive();

            // Chill
            ai.StopMoving();

            // Try play voice
            TryPlayCurrentStateVoiceAudio(ai);

            // Crouch
            ai.FollowCrouchIfCanDo();

            if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
                LookAtWhatPlayerPointingAt(ai);
            else
            {
                // Emotes
                ai.NpcController.MimicEmotes(ai.targetPlayer);
            }


            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            EnumVoicesState voiceState = EnumVoicesState.Chilling;

            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = voiceState,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        private IEnumerator ChillLookRoutine()
        {
            ai.NpcController.OrderToLookForward();

            while (true)
            {
                FindTargetToLookAt();

                // Pause
                yield return new WaitForSeconds(Random.Range(idlePauseRange.x, idlePauseRange.y));

                // Check if we stop looking at current target
                if (currentLookTarget != null)
                {
                    float now = Time.time;
                    if (lastTime < 0f)
                        lastTime = now;

                    float delta = now - lastTime;
                    lastTime = now;

                    timerLookingAtTarget -= delta;
                    if (timerLookingAtTarget < 0f)
                        currentLookTarget = null!;
                    else
                        continue;
                }

                // No target to look at
                // --------------------

                float choice = Random.value;
                // 30% look at player
                if (choice < 0.30f)
                {
                    ai.NpcController.OrderToLookAtPlayer(StartOfRound.Instance.localPlayerController.playerEye.transform.position);
                }
                // 30% random look
                else if (choice < 0.60f)
                {
                    Vector3 lookPos = GetRandomLookPosition();
                    ai.NpcController.OrderToLookAtPosition(lookPos);
                }
                // 10% look forward
                else if (choice < 0.7f)
                {
                    ai.NpcController.OrderToLookForward();
                }
                // 30% nothing
                else
                {
                    // empty
                }

                yield return new WaitForSeconds(Random.Range(lookDurationRange.x, lookDurationRange.y));
            }
        }

        private Vector3 GetRandomLookPosition()
        {
            float angle = Random.Range(-randomLookAngle, randomLookAngle);
            Quaternion rot = Quaternion.Euler(0, angle, 0);

            Vector3 dir = rot * ai.Npc.transform.forward;
            return ai.Npc.transform.position + dir * randomLookDistance;
        }

        private void FindTargetToLookAt()
        {
            // Player or interns
            foreach (PlayerControllerB playerOrIntern in StartOfRound.Instance.allPlayerScripts)
            {
                if (playerOrIntern == null
                   || playerOrIntern.isPlayerDead
                   || playerOrIntern == StartOfRound.Instance.localPlayerController
                   || playerOrIntern.playerClientId == ai.Npc.playerClientId
                   || (InternManager.Instance.IsPlayerIntern(playerOrIntern) && playerOrIntern.OwnerClientId == StartOfRound.Instance.localPlayerController.actualClientId))
                    continue;

                TrySetLookTarget(playerOrIntern.playerEye.transform ?? playerOrIntern.transform);
            }

            // Enemies
            foreach (EnemyAI enemy in InternManager.Instance.GetEnemiesList())
            {
                if (enemy == null
                   || enemy.isEnemyDead
                   || enemy is InternAI)
                    continue;

                TrySetLookTarget(enemy.eye ?? enemy.transform);
            }
        }

        private void TrySetLookTarget(Transform candidate)
        {
            if (candidate == null)
                return;

            if (!IsTargetValid(candidate))
                return;

            currentLookTarget = candidate;
            ai.NpcController.OrderToLookAtPlayer(currentLookTarget.position);
            timerLookingAtTarget = Random.Range(1f, 10f);
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null)
                return false;

            if (!IsInRange(target))
                return false;

            if (!HasLineOfSightOn(target))
                return false;

            return true;
        }

        private bool IsInRange(Transform target)
        {
            float distanceFromTarget = GetSqrDistanceFrom(target);

            bool isInRange = distanceFromTarget <= maxLookDistance * maxLookDistance;
            if (!isInRange)
                return false;
            if (currentLookTarget == null)
                return true;
            // New target closer
            return distanceFromTarget < GetSqrDistanceFrom(currentLookTarget);
        }

        private float GetSqrDistanceFrom(Transform target)
        {
            return (target.position - ai.Npc.transform.position).sqrMagnitude;
        }

        private bool HasLineOfSightOn(Transform target)
        {
            return !Physics.Linecast(ai.Npc.playerEye.transform.position,
                                     target.position + new Vector3(0f, 1f, 0f),
                                     StartOfRound.Instance.collidersAndRoomMaskAndDefault,
                                     QueryTriggerInteraction.Ignore);
        }

        private void LookAtWhatPlayerPointingAt(InternAI ai)
        {
            // Look where the target player is looking
            Ray interactRay = new Ray(ai.targetPlayer.gameplayCamera.transform.position, ai.targetPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                ai.NpcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                ai.NpcController.OrderToLookForward();
            }
            else
            {
                // Check if looking at a player/intern
                foreach (var hit in raycastHits)
                {
                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null
                        && player.playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        ai.NpcController.OrderToLookAtPosition(hit.point);
                        ai.NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        ai.NpcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                        ai.NpcController.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    ai.NpcController.OrderToLookAtPosition(hit.point);
                    ai.NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }
    }
}
