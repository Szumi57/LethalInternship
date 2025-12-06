using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class FleeFromEnemy : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            CoroutineController panikCoroutine = context.PanikCoroutine;
            PluginLoggerHook.LogDebug?.Invoke("FleeFromEnemy Action ??????????????????");

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("FleeFromEnemy Action, CurrentEnemy is null");
                return BehaviourTreeStatus.Failure;
            }

            float? fearRange = ai.GetFearRangeForEnemies(context.CurrentEnemy);
            if (!fearRange.HasValue)
            {
                PluginLoggerHook.LogDebug?.Invoke($"FleeFromEnemy fearRange is null, ignoring enemy \"{context.CurrentEnemy.enemyType.enemyName}\"");
                panikCoroutine.StopCoroutine();
                context.CurrentEnemy = null;
                return BehaviourTreeStatus.Success;
            }

            // Keep coroutine
            panikCoroutine.KeepAlive();

            // Check to see if the intern can see the enemy, or enemy has line of sight to intern
            float sqrDistanceToEnemy = (ai.NpcController.Npc.transform.position - context.CurrentEnemy.transform.position).sqrMagnitude;
            if (Physics.Linecast(context.CurrentEnemy.transform.position, ai.NpcController.Npc.gameplayCamera.transform.position,
                                 StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                // If line of sight broke
                // and the intern is far enough when the enemy can not see him
                if (sqrDistanceToEnemy > Const.DISTANCE_FLEEING_NO_LOS * Const.DISTANCE_FLEEING_NO_LOS)
                {
                    panikCoroutine.StopCoroutine();
                    context.CurrentEnemy = null;
                    return BehaviourTreeStatus.Success;
                }
            }
            // Enemy still has line of sight of intern

            // Far enough from enemy
            if (sqrDistanceToEnemy > fearRange * fearRange)
            {
                context.CurrentEnemy = null;
                panikCoroutine.StopCoroutine();
                return BehaviourTreeStatus.Success;
            }
            // Enemy still too close

            // If enemy still too close, and destination reached, restart the panic routine
            if ((ai.destination - ai.NpcController.Npc.transform.position).sqrMagnitude < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                panikCoroutine.RestartCoroutine(ChooseFleeingNodeFromPosition(ai, context.CurrentEnemy.transform, fearRange.Value));
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

        /// <summary>
        /// Coroutine to find the closest node after some distance (see: <see cref="InternAI.GetFearRangeForEnemies"><c>InternAI.GetFearRangeForEnemies</c></see>).
        /// In other word, find a path node to flee from the enemy.
        /// </summary>
        /// <remarks>Or should I say an attempt to code it.</remarks>
        /// <param name="enemyTransform">Position of the enemy</param>
        /// <returns></returns>
        private IEnumerator ChooseFleeingNodeFromPosition(InternAI ai, Transform enemyTransform, float fearRange)
        {
            var nodes = ai.allAINodes.OrderBy(node => (node.transform.position - ai.transform.position).sqrMagnitude)
                                     .ToArray();
            yield return null;

            // no need for a loop I guess
            for (var i = 0; i < nodes.Length; i++)
            {
                Transform nodeTransform = nodes[i].transform;

                if ((nodeTransform.position - enemyTransform.position).sqrMagnitude < fearRange * fearRange)
                {
                    continue;
                }

                if (!ai.agent.CalculatePath(nodeTransform.position, ai.path1))
                {
                    yield return null;
                    continue;
                }

                // Assign destination
                ai.SetDestinationToPositionInternAI(nodeTransform.position);
                break;
            }

            yield break;
        }
    }
}
