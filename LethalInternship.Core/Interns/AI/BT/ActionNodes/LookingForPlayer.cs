using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class LookingForPlayer : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Start coroutine for wandering
            context.searchingWanderCoroutineController.StartCoroutine(SearchingWander(ai));
            context.searchingWanderCoroutineController.KeepAlive();

            // Start the coroutine from base game to search for players
            context.searchForPlayers.StartSearch(ai.NpcController.Npc.transform.position);
            context.searchForPlayers.KeepAlive();

            ai.SetDestinationToPositionInternAI(ai.destination);
            ai.OrderAgentAndBodyMoveToDestination();

            return BehaviourTreeStatus.Success;
        }

        /// <summary>
        /// Coroutine for when searching, alternate between sprinting and walking
        /// </summary>
        /// <remarks>
        /// The other coroutine <see cref="EnemyAI.StartSearch"><c>EnemyAI.StartSearch</c></see>, already take care of choosing node to walk to.
        /// </remarks>
        /// <returns></returns>
        private IEnumerator SearchingWander(InternAI ai)
        {
            yield return null;
            while (ai.targetPlayer == null)
            {
                float freezeTimeRandom = Random.Range(Const.MIN_TIME_SPRINT_SEARCH_WANDER, Const.MAX_TIME_SPRINT_SEARCH_WANDER);
                ai.NpcController.OrderToSprint();
                yield return new WaitForSeconds(freezeTimeRandom);

                freezeTimeRandom = Random.Range(Const.MIN_TIME_SPRINT_SEARCH_WANDER, Const.MAX_TIME_SPRINT_SEARCH_WANDER);
                ai.NpcController.OrderToStopSprint();
                yield return new WaitForSeconds(freezeTimeRandom);
            }
        }
    }
}
