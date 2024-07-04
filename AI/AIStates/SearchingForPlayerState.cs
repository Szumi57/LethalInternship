using GameNetcodeStuff;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class SearchingForPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.SearchingForPlayer;
        public override EnumAIStates GetAIState() { return STATE; }

        private PlayerControllerB? player;
        private Coroutine searchingWanderCoroutine = null!;

        public SearchingForPlayerState(AIState newState) : base(newState) { }
        public SearchingForPlayerState(InternAI ai) : base(ai) { }

        public override void DoAI()
        {
            StartSearchingWanderCoroutine();

            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                StopSearchingWanderCoroutine();
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // Check for object to grab
            if (ai.AreHandsFree())
            {
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    StopSearchingWanderCoroutine();
                    ai.State = new FetchingObjectState(this, grabbableObject);
                    return;
                }
            }

            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                // new target
                StopSearchingWanderCoroutine();
                ai.SyncAssignTargetAndSetMovingTo(player);
                return;
            }

            ai.OrderMoveToDestination();

            if (!searchForPlayers.inProgress)
            {
                ai.StartSearch(ai.transform.position, searchForPlayers);
            }
        }

        private IEnumerator SearchingWander()
        {
            yield return null;
            while (ai.State != null 
                    && ai.State.GetAIState() == EnumAIStates.SearchingForPlayer)
            {
                float freezeTimeRandom = Random.Range(Const.MIN_TIME_SPRINT_SEARCH_WANDER, Const.MAX_TIME_SPRINT_SEARCH_WANDER);
                npcController.OrderToSprint();
                yield return new WaitForSeconds(freezeTimeRandom);

                freezeTimeRandom = Random.Range(Const.MIN_TIME_SPRINT_SEARCH_WANDER, Const.MAX_TIME_SPRINT_SEARCH_WANDER);
                npcController.OrderToStopSprint();
                yield return new WaitForSeconds(freezeTimeRandom);
            }
        }

        private void StartSearchingWanderCoroutine()
        {
            if (this.searchingWanderCoroutine == null)
            {
                this.searchingWanderCoroutine = ai.StartCoroutine(this.SearchingWander());
            }
        }

        private void StopSearchingWanderCoroutine()
        {
            if (this.searchingWanderCoroutine != null)
            {
                ai.StopCoroutine(this.searchingWanderCoroutine);
            }
        }
    }
}
