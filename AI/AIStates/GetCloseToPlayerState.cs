using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the intern has a target player and wants to get close to him.
    /// </summary>
    internal class GetCloseToPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.GetCloseToPlayer;
        /// <summary>
        /// <inheritdoc cref="AIState.GetAIState"/>
        /// </summary>
        public override EnumAIStates GetAIState() { return STATE; }

        /// <summary>
        /// Represents the distance between the body of intern (<c>PlayerControllerB</c> position) and the target player (owner of intern), 
        /// only on axis x and z, y at 0, and squared
        /// </summary>
        private float SqrHorizontalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        /// <summary>
        /// Represents the distance between the body of intern (<c>PlayerControllerB</c> position) and the target player (owner of intern), 
        /// only on axis y, x and z at 0, and squared
        /// </summary>
        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
            }
        }

        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public GetCloseToPlayerState(AIState state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        /// <summary>
        /// <inheritdoc cref="AIState(InternAI)"/>
        /// </summary>
        public GetCloseToPlayerState(InternAI ai) : base(ai) 
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
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

            // Lost target player
            if (ai.targetPlayer == null)
            {
                // Last position unknown
                if (this.targetLastKnownPosition.HasValue)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }

                ai.State = new SearchingForPlayerState(this);
                return;
            }

            if (!ai.PlayerIsTargetable(ai.targetPlayer, false, true))
            {
                // Target is not available anymore
                ai.State = new SearchingForPlayerState(this);
                return;
            }

            // Target in ship, wait outside
            if (ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new PlayerInShipState(this);
                return;
            }

            // Check for object to grab
            if (ai.AreHandsFree())
            {
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    ai.State = new FetchingObjectState(this, grabbableObject);
                    return;
                }
            }

            // Target is in awarness range
            if (SqrHorizontalDistanceWithTarget < Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    && SqrVerticalDistanceWithTarget < Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.SyncAssignTargetAndSetMovingTo(ai.targetPlayer);
            }
            else
            {
                // Target outside of awareness range, if ai does not see target, then the target is lost
                Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} no see target, still in range ? too far {SqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {SqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                PlayerControllerB? checkTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (checkTarget == null)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
                else
                {
                    targetLastKnownPosition = ai.targetPlayer.transform.position;
                    ai.SyncAssignTargetAndSetMovingTo(ai.targetPlayer);
                }
            }

            // Follow player
            // Sprint if far, stop sprinting if close
            // If close enough, chill with player
            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                || SqrVerticalDistanceWithTarget > 0.3f * 0.3f)
            {
                npcController.OrderToSprint();
            }
            else if (SqrHorizontalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                     && SqrVerticalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.State = new ChillWithPlayerState(this);
                return;
            }
            else if (SqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
            {
                npcController.OrderToStopSprint();
            }

            ai.OrderMoveToDestination();
        }
    }
}
