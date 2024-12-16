using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the interns try to get close and grab a item
    /// </summary>
    internal class FetchingObjectState : AIState
    {
        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public FetchingObjectState(AIState state, GrabbableObject targetItem) : base(state)
        {
            CurrentState = EnumAIStates.FetchingObject;

            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }

            this.targetItem = targetItem;
        }

        /// <summary>
        /// <inheritdoc cref="AIState.DoAI"/>
        /// </summary>
        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // Target item invalid to grab
            if (ai.HeldItem != null
                || this.targetItem == null 
                || !ai.IsGrabbableObjectGrabbable(this.targetItem))
            {
                this.targetItem = null;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            float sqrMagDistanceItem = (this.targetItem.transform.position - npcController.Npc.transform.position).sqrMagnitude;
            // Close enough to item for grabbing, attempt to grab
            if (sqrMagDistanceItem < npcController.Npc.grabDistance * npcController.Npc.grabDistance * Plugin.Config.InternSizeScale.Value)
            {
                if (!npcController.Npc.inAnimationWithEnemy 
                    && !npcController.Npc.activatingItem)
                {
                    ai.GrabItemServerRpc(this.targetItem.NetworkObject, itemGiven: false);
                    this.targetItem = null;
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }
            }

            // Else get close to item
            ai.SetDestinationToPositionInternAI(this.targetItem.transform.position);

            // Look at item or not if hidden by stuff
            if (!Physics.Linecast(npcController.Npc.gameplayCamera.transform.position, this.targetItem.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                npcController.OrderToLookAtPosition(this.targetItem.transform.position);
            }
            else
            {
                npcController.OrderToLookForward();
            }

            // Sprint if far enough from the item
            if (sqrMagDistanceItem > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            {
                npcController.OrderToSprint();
            }

            ai.OrderMoveToDestination();
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            // Talk if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FoundLoot,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = npcController.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public override string GetBillboardStateIndicator()
        {
            return "!!";
        }
    }
}
