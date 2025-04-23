using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.Interns.AI.Commands
{
    public class LookingForPlayerCommand : ICommandAI
    {
        private readonly InternAI ai;
        private NpcController Controller { get { return ai.NpcController; } }
        private AISearchRoutine SearchForPlayers { get { return ai.SearchForPlayers; } set { ai.SearchForPlayers = value; } }
        private Vector3? TargetLastKnownPosition { set { ai.TargetLastKnownPosition = value; } }

        private Coroutine SearchingWanderCoroutine { get { return ai.SearchingWanderCoroutine; } set { ai.SearchingWanderCoroutine = value; } }

        public LookingForPlayerCommand(InternAI ai)
        {
            this.ai = ai;
        }

        public void Execute()
        {
            // Start coroutine for wandering
            StartSearchingWanderCoroutine();

            // Try to find the closest player to target
            PlayerControllerB? player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null) // new target
            {
                // Play voice
                ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                {
                    VoiceState = EnumVoicesState.LostAndFound,
                    CanTalkIfOtherInternTalk = true,
                    WaitForCooldown = false,
                    CutCurrentVoiceStateToTalk = true,
                    CanRepeatVoiceState = false,

                    ShouldSync = true,
                    IsInternInside = Controller.Npc.isInsideFactory,
                    AllowSwearing = Plugin.Config.AllowSwearing.Value
                });

                // Assign to new target
                StopSearchingWanderCoroutine();
                ai.SyncAssignTargetAndSetMovingTo(player);
                if (Plugin.Config.ChangeSuitAutoBehaviour.Value)
                {
                    ai.ChangeSuitInternServerRpc(Controller.Npc.playerClientId, player.currentSuitID);
                }

                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return;
            }

            ai.SetDestinationToPositionInternAI(ai.destination);
            ai.OrderAgentAndBodyMoveToDestination();

            if (!SearchForPlayers.inProgress)
            {
                // Start the coroutine from base game to search for players
                ai.StartSearch(ai.NpcController.Npc.transform.position, SearchForPlayers);
            }

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return;
        }

        public string GetBillboardStateIndicator()
        {
            return "?";
        }

        public void PlayerHeard(Vector3 noisePosition)
        {
            // Go towards the sound heard
            TargetLastKnownPosition = noisePosition;
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

            ai.QueueNewCommand(new LostPlayerCommand(ai));
        }

        private void TryPlayCurrentStateVoiceAudio()
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
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
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
                Controller.OrderToSprint();
                yield return new WaitForSeconds(freezeTimeRandom);

                freezeTimeRandom = Random.Range(Const.MIN_TIME_SPRINT_SEARCH_WANDER, Const.MAX_TIME_SPRINT_SEARCH_WANDER);
                Controller.OrderToStopSprint();
                yield return new WaitForSeconds(freezeTimeRandom);
            }
        }

        private void StartSearchingWanderCoroutine()
        {
            if (SearchingWanderCoroutine == null)
            {
                SearchingWanderCoroutine = ai.StartCoroutine(SearchingWander());
            }
        }

        private void StopSearchingWanderCoroutine()
        {
            if (SearchingWanderCoroutine != null)
            {
                ai.StopCoroutine(SearchingWanderCoroutine);
            }
        }

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.LookingForPlayer;
        }
    }
}
