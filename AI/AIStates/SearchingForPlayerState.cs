using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the intern has no target player to follow, and is looking for one.
    /// </summary>
    /// <remarks>
    /// The owner of the intern in this state is the last one that owns it before changing to this state, 
    /// the host if no one, just after spawn for example.
    /// </remarks>
    internal class SearchingForPlayerState : AIState
    {
        private PlayerControllerB? player;
        private Coroutine searchingWanderCoroutine = null!;

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public SearchingForPlayerState(AIState oldState) : base(oldState)
        {
            CurrentState = EnumAIStates.SearchingForPlayer;
        }
        /// <summary>
        /// <inheritdoc cref="AIState(InternAI)"/>
        /// </summary>
        public SearchingForPlayerState(InternAI ai) : base(ai)
        {
            CurrentState = EnumAIStates.SearchingForPlayer;
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            // Start coroutine for wandering
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

            // Try to find the closest player to target
            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                // new target
                StopSearchingWanderCoroutine();
                ai.SyncAssignTargetAndSetMovingTo(player);
                if (Plugin.Config.ChangeSuitBehaviour.Value == (int)EnumOptionSuitChange.AutomaticSameAsPlayer)
                {
                    ai.ChangeSuitInternServerRpc(npcController.Npc.playerClientId, player.currentSuitID);
                }
                return;
            }

            ai.SetDestinationToPositionInternAI(ai.destination);
            ai.OrderMoveToDestination();

            if (!searchForPlayers.inProgress)
            {
                // Start the coroutine from base game to search for players
                ai.StartSearch(ai.NpcController.Npc.transform.position, searchForPlayers);
            }
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.Lost,
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
            // Go towards the sound heard
            this.targetLastKnownPosition = noisePosition;
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

            ai.State = new JustLostPlayerState(this);
        }

        public override string GetBillboardStateIndicator()
        {
            return "?";
        }

        /// <summary>
        /// Coroutine for when searching, alternate between sprinting and walking
        /// </summary>
        /// <remarks>
        /// The other coroutine <see cref="EnemyAI.StartSearch"><c>EnemyAI.StartSearch</c></see>, already take care of choosing node to walk to.
        /// </remarks>
        /// <returns></returns>
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
