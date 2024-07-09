using LethalInternship.Enums;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the intern just saw a dangerous enemy (see: <see cref="InternAI.GetFearRangeForEnemies"><c>InternAI.GetFearRangeForEnemies</c></see>).
    /// The intern try to flee by choosing a far away node from the enemy.
    /// </summary>
    internal class PanikState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.Panik;
        /// <summary>
        /// <inheritdoc cref="AIState.GetAIState"/>
        /// </summary>
        public override EnumAIStates GetAIState() { return STATE; }

        /// <summary>
        /// Represent the distance between the body of intern (<c>PlayerControllerB</c> position) and the destination point, squared
        /// </summary>
        private float SqrDistanceToDestination
        {
            get
            {
                return (ai.destination - npcController.Npc.transform.position).sqrMagnitude;
            }

        }

        /// <summary>
        /// Constructor for PanikState
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="enemyAI">EnemyAI to flee</param>
        public PanikState(AIState oldState, EnemyAI enemyAI) : base(oldState)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }

            Plugin.Logger.LogDebug($"{npcController.Npc.playerUsername} enemy seen {enemyAI.enemyType.enemyName}");
            this.enemyTransform = enemyAI.transform;
            StartPanikCoroutine(this.enemyTransform);
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            if (enemyTransform == null)
            {
                return;
            }

            // Check to see if the intern can see the enemy, or enemy has line of sight to intern
            if (Physics.Linecast(enemyTransform.position, npcController.Npc.gameplayCamera.transform.position,
                                 StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                // If line of sight broke
                // and the intern is far enough when the enemy can not see him
                if ((npcController.Npc.transform.position - enemyTransform.position).sqrMagnitude > Const.DISTANCE_FLEEING_NO_LOS * Const.DISTANCE_FLEEING_NO_LOS)
                {
                    ai.State = new GetCloseToPlayerState(this);
                    StopPanikCoroutine();
                    return;
                }
            }
            // Enemy still has line of sight of intern

            // Far enough from enemy
            if ((npcController.Npc.transform.position - enemyTransform.position).sqrMagnitude > Const.DISTANCE_FLEEING * Const.DISTANCE_FLEEING)
            {
                ai.State = new GetCloseToPlayerState(this);
                StopPanikCoroutine();
                return;
            }

            // If enemy still too close, restart the panic routine
            if (SqrDistanceToDestination < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                RestartPanikCoroutine(this.enemyTransform);
            }

            // Sprint of course
            npcController.OrderToSprint();
            ai.OrderMoveToDestination();
        }

        /// <summary>
        /// Coroutine to find the closest node after some distance (<see cref="Const.DISTANCE_FLEEING"><c>Const.DISTANCE_FLEEING</c></see>).
        /// In other word, find a path node to flee from the enemy.
        /// </summary>
        /// <remarks>Or should I say an attempt to code it.</remarks>
        /// <param name="enemyTransform">Position of the enemy</param>
        /// <returns></returns>
        private IEnumerator ChooseFleeingNodeFromPosition(Transform enemyTransform)
        {
            var nodes = ai.allAINodes.OrderBy(node => (node.transform.position - this.ai.transform.position).sqrMagnitude)
                                     .ToArray();
            yield return null;

            // no need for a loop I guess
            for (var i = 0; i < nodes.Length; i++)
            {
                Transform nodeTransform = nodes[i].transform;

                if ((nodeTransform.position - enemyTransform.position).sqrMagnitude < Const.DISTANCE_FLEEING * Const.DISTANCE_FLEEING)
                {
                    continue;
                }

                if (!this.ai.agent.CalculatePath(nodeTransform.position, this.ai.path1))
                {
                    yield return null;
                    continue;
                }

                ai.SetDestinationToPositionInternAI(nodeTransform.position);
                yield break;
            }
        }

        private void StartPanikCoroutine(Transform enemyTransform)
        {
            panikCoroutine = ai.StartCoroutine(ChooseFleeingNodeFromPosition(enemyTransform));
        }

        private void RestartPanikCoroutine(Transform enemyTransform)
        {
            StopPanikCoroutine();
            StartPanikCoroutine(enemyTransform);
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
