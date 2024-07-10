using LethalInternship.Enums;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the interns try to get close and grab a item
    /// </summary>
    internal class FetchingObjectState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.FetchingObject;
        /// <summary>
        /// <inheritdoc cref="AIState.GetAIState"/>
        /// </summary>
        public override EnumAIStates GetAIState() { return STATE; }

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public FetchingObjectState(AIState state, GrabbableObject targetItem) : base(state)
        {
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
            if (this.targetItem == null 
                || !ai.IsGrabbableObjectGrabbable(this.targetItem))
            {
                this.targetItem = null;
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} try to grab {this.targetItem.name}");
            // Close enough to item for grabbing, attempt to grab
            if ((this.targetItem.transform.position - npcController.Npc.transform.position).sqrMagnitude < npcController.Npc.grabDistance * npcController.Npc.grabDistance * Const.SIZE_SCALE_INTERN)
            {
                if (!npcController.Npc.inAnimationWithEnemy && !npcController.Npc.activatingItem)
                {
                    ai.GrabItemServerRpc(this.targetItem.NetworkObject);
                    this.targetItem = null;
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }
            }

            // Else get close to item
            ai.SetDestinationToPositionInternAI(this.targetItem.transform.position);
            npcController.OrderToLookAtPosition(this.targetItem.transform.position);

            // Sprint if far enough from the item
            if ((this.targetItem.transform.position - npcController.Npc.transform.position).sqrMagnitude > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING)
            {
                npcController.OrderToSprint();
            }

            ai.OrderMoveToDestination();
        }
    }
}
