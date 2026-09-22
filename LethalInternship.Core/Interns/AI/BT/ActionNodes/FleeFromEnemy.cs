using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class FleeFromEnemy : IBTAction
    {
        private float fleeRepathInterval = 0.5f;
        private float nextFleeUpdateTime;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("FleeFromEnemy Action, CurrentEnemy is null");
                return BehaviourTreeStatus.Failure;
            }

            float? fearRange = ai.GetFearRangeForEnemies(context.CurrentEnemy);
            if (!fearRange.HasValue)
            {
                PluginLoggerHook.LogDebug?.Invoke($"FleeFromEnemy fearRange is null, ignoring enemy \"{context.CurrentEnemy.enemyType.enemyName}\"");
                return BehaviourTreeStatus.Success;
            }

            // Check to see if the intern can see the enemy, or enemy has line of sight to intern
            float sqrDistanceToEnemy = (ai.NpcController.Npc.transform.position - context.CurrentEnemy.transform.position).sqrMagnitude;
            if (Physics.Linecast(context.CurrentEnemy.transform.position, ai.NpcController.Npc.gameplayCamera.transform.position,
                                 StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                // If line of sight broke
                // and the intern is far enough when the enemy can not see him
                //Debug.Log($"No Linecast DistanceToEnemy {Mathf.Sqrt(sqrDistanceToEnemy)} <? {Const.DISTANCE_FLEEING_NO_LOS}");
                if (sqrDistanceToEnemy > Const.DISTANCE_FLEEING_NO_LOS * Const.DISTANCE_FLEEING_NO_LOS)
                {
                    return BehaviourTreeStatus.Success;
                }
            }
            // Enemy still has line of sight of intern

            // Far enough from enemy
            if (sqrDistanceToEnemy > fearRange * fearRange)
            {
                return BehaviourTreeStatus.Success;
            }
            // Enemy still too close

            if (Time.time > nextFleeUpdateTime)
            {
                nextFleeUpdateTime = Time.time + fleeRepathInterval;

                // Search for node to flee to
                UpdateNodeToFleeTo(context, fearRange.Value);
            }

            // Sprint of course
            ai.NpcController.OrderToSprint();
            ai.OrderAgentAndBodyMoveToDestination();

            // Try play voice
            TryPlayCurrentStateVoiceAudio(ai);

            // Crouch
            ai.FollowCrouchIfCanDo(panik: true);

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Priority state
            // Stop talking and voice new state
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.RunningFromMonster,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        private void UpdateNodeToFleeTo(BTContext context, float fearRange)
        {
            InternAI ai = context.InternAI;

            Vector3 npcPos = ai.Npc.transform.position;
            Vector3 enemyPos = context.CurrentEnemy!.transform.position;

            Vector3 baseDir = (npcPos - enemyPos).normalized;

            Vector3[] dirs =
            {
                baseDir,
                Quaternion.Euler(0, -60f, 0) * baseDir,
                Quaternion.Euler(0,  60f, 0) * baseDir
            };

            foreach (var dir in dirs)
            {
                if (TrySetFleeDestination(ai, dir, fearRange))
                    return;
            }
        }

        bool TrySetFleeDestination(InternAI ai, Vector3 dir, float fleeDistance)
        {
            Vector3 target = ai.Npc.transform.position + dir * fleeDistance;

            if (!NavMesh.SamplePosition(target, out var hit, 6f, NavMesh.AllAreas))
                return false;

            ai.SetDestinationToPositionInternAI(hit.position);

            return ai.agent.pathStatus == NavMeshPathStatus.PathComplete;
        }
    }
}
