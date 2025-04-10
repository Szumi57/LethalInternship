using LethalInternship.Constants;
using LethalInternship.Enums;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Interns.AI.AIStates
{
    /// <summary>
    /// State where the intern just saw a dangerous enemy (see: <see cref="InternAI.GetFearRangeForEnemies"><c>InternAI.GetFearRangeForEnemies</c></see>).
    /// The intern try to flee by choosing a far away node from the enemy.
    /// </summary>
    public class PanikState : AIState
    {
        private Coroutine? panikCoroutine;
        private EnemyAI? currentEnemy;

        /// <summary>
        /// Constructor for PanikState
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="enemyAI">EnemyAI to flee</param>
        public PanikState(AIState oldState, EnemyAI enemyAI) : base(oldState)
        {
            CurrentState = EnumAIStates.Panik;

            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }

            Plugin.LogDebug($"{npcController.Npc.playerUsername} enemy seen {enemyAI.enemyType.enemyName}");
            currentEnemy = enemyAI;
            float? fearRange = ai.GetFearRangeForEnemies(currentEnemy);
            if (fearRange.HasValue)
            {
                StartPanikCoroutine(currentEnemy.transform, fearRange.Value);
            }
            else
            {
                ai.State = new RelaxState(this);
            }
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            if (currentEnemy == null)
            {
                ai.State = new RelaxState(this);
                StopPanikCoroutine();
                return;
            }

            float? fearRange = ai.GetFearRangeForEnemies(currentEnemy);
            if (!fearRange.HasValue)
            {
                ai.State = new RelaxState(this);
                StopPanikCoroutine();
                return;
            }

            // Check if another enemy is closer
            EnemyAI? newEnemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (newEnemyAI != null)
            {
                float? newFearRange = ai.GetFearRangeForEnemies(newEnemyAI);
                if (newFearRange.HasValue)
                {
                    currentEnemy = newEnemyAI;
                    fearRange = newFearRange.Value;
                    RestartPanikCoroutine(currentEnemy, fearRange.Value);
                }
                // else no fear range, ignore this enemy, already ignored by CheckLOSForEnemy but hey better be safe
            }

            // Check to see if the intern can see the enemy, or enemy has line of sight to intern
            float sqrDistanceToEnemy = (npcController.Npc.transform.position - currentEnemy.transform.position).sqrMagnitude;
            if (Physics.Linecast(currentEnemy.transform.position, npcController.Npc.gameplayCamera.transform.position,
                                 StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                // If line of sight broke
                // and the intern is far enough when the enemy can not see him
                if (sqrDistanceToEnemy > Const.DISTANCE_FLEEING_NO_LOS * Const.DISTANCE_FLEEING_NO_LOS)
                {
                    ai.State = new RelaxState(this);
                    StopPanikCoroutine();
                    return;
                }
            }
            // Enemy still has line of sight of intern

            // Far enough from enemy
            if (sqrDistanceToEnemy > fearRange * fearRange)
            {
                ai.State = new RelaxState(this);
                StopPanikCoroutine();
                return;
            }
            // Enemy still too close

            // If enemy still too close, and destination reached, restart the panic routine
            if ((ai.destination - npcController.Npc.transform.position).sqrMagnitude < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                RestartPanikCoroutine(currentEnemy, fearRange.Value);
            }

            // Sprint of course
            npcController.OrderToSprint();
            ai.OrderAgentAndBodyMoveToDestination();

            // Try play voice
            TryPlayCurrentStateVoiceAudio();
        }

        public override void TryPlayCurrentStateVoiceAudio()
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
                IsInternInside = npcController.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public override string GetBillboardStateIndicator()
        {
            return @"/!\";
        }

        /// <summary>
        /// Coroutine to find the closest node after some distance (see: <see cref="InternAI.GetFearRangeForEnemies"><c>InternAI.GetFearRangeForEnemies</c></see>).
        /// In other word, find a path node to flee from the enemy.
        /// </summary>
        /// <remarks>Or should I say an attempt to code it.</remarks>
        /// <param name="enemyTransform">Position of the enemy</param>
        /// <returns></returns>
        private IEnumerator ChooseFleeingNodeFromPosition(Transform enemyTransform, float fearRange)
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

        private void StartPanikCoroutine(Transform enemyTransform, float fearRange)
        {
            panikCoroutine = ai.StartCoroutine(ChooseFleeingNodeFromPosition(enemyTransform, fearRange));
        }

        private void RestartPanikCoroutine(EnemyAI currentEnemy, float fearRange)
        {
            StopPanikCoroutine();
            StartPanikCoroutine(currentEnemy.transform, fearRange);
        }

        private void StopPanikCoroutine()
        {
            if (panikCoroutine != null)
            {
                ai.StopCoroutine(panikCoroutine);
            }
        }
    }
}
