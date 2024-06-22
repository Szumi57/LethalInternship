using LethalInternship.Enums;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class PanikState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.Panik;

        private float SqrDistanceToDestination
        {
            get
            {
                return (ai.destination - npcController.Npc.transform.position).sqrMagnitude;
            }

        }

        public PanikState(AIState newState, EnemyAI enemyAI) : base(newState)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }

            Plugin.Logger.LogDebug($"{npcController.Npc.playerUsername} enemy seen {enemyAI.enemyType.enemyName}");
            this.enemyTransform = enemyAI.transform;
            StartPanikCoroutine(this.enemyTransform);
        }

        public override EnumAIStates GetAIState() { return STATE; }

        public override void DoAI()
        {
            if (enemyTransform == null)
            {
                return;
            }

            if (Physics.Linecast(enemyTransform.position, npcController.Npc.gameplayCamera.transform.position,
                                 StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                // Line of sight broke
                if ((npcController.Npc.transform.position - enemyTransform.position).sqrMagnitude > Const.DISTANCE_FLEEING_NO_LOS * Const.DISTANCE_FLEEING_NO_LOS)
                {
                    ai.State = new GetCloseToPlayerState(this);
                    StopPanikCoroutine();
                    return;
                }
            }

            // Far enough from enemy
            if ((npcController.Npc.transform.position - enemyTransform.position).sqrMagnitude > Const.DISTANCE_FLEEING * Const.DISTANCE_FLEEING)
            {
                ai.State = new GetCloseToPlayerState(this);
                StopPanikCoroutine();
                return;
            }

            if (SqrDistanceToDestination < Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION * Const.DISTANCE_CLOSE_ENOUGH_TO_DESTINATION)
            {
                RestartPanikCoroutine(this.enemyTransform);
            }

            npcController.OrderToSprint();
            ai.OrderMoveToDestination();
        }

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
