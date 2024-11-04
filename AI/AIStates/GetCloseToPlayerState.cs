using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// State where the intern has a target player and wants to get close to him.
    /// </summary>
    internal class GetCloseToPlayerState : AIState
    {
        /// <summary>
        /// <inheritdoc cref="AIState(AIState)"/>
        /// </summary>
        public GetCloseToPlayerState(AIState state) : base(state)
        {
            CurrentState = EnumAIStates.GetCloseToPlayer;

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
            CurrentState = EnumAIStates.GetCloseToPlayer;

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
            if (ai.IsPlayerInShipBoundsExpanded(ai.targetPlayer))
            {
                ai.State = new PlayerInShipState(this);
                return;
            }

            VehicleController? vehicleController = ai.GetVehicleCruiserTargetPlayerIsIn();
            if (vehicleController != null)
            {
                ai.State = new PlayerInCruiserState(this, vehicleController);
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
            float sqrHorizontalDistanceWithTarget = Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistanceWithTarget = Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    && sqrVerticalDistanceWithTarget < Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.SyncAssignTargetAndSetMovingTo(ai.targetPlayer);
            }
            else
            {
                // Target outside of awareness range, if ai does not see target, then the target is lost
                //Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} no see target, still in range ? too far {sqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {sqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                PlayerControllerB? checkTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (checkTarget == null)
                {
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
                else
                {
                    // Target still visible
                    targetLastKnownPosition = ai.targetPlayer.transform.position;
                    ai.SyncAssignTargetAndSetMovingTo(ai.targetPlayer);

                    // Bring closer with teleport if possible
                    ai.CheckAndBringCloserTeleportIntern(0.8f);
                }
            }

            // Follow player
            // If close enough, chill with player
            // Sprint if far, stop sprinting if close
            if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.State = new ChillWithPlayerState(this);
                return;
            }
            else if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                     || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
            {
                npcController.OrderToSprint();
            }
            else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
            {
                npcController.OrderToStopSprint();
            }

            ai.OrderMoveToDestination();
        }

        public override void TryPlayVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            if (InternManager.Instance.DidAnInternJustTalkedClose(ai))
            {
                ai.InternIdentity.Voice.SetNewRandomCooldownAudio();
                return;
            }

            if (!ai.InternIdentity.Voice.CanPlayAudio())
            {
                return;
            }

            ai.InternIdentity.Voice.PlayRandomVoiceAudio(EnumVoicesState.FollowingPlayer);
            lastVoiceState = EnumVoicesState.FollowingPlayer;
        }
    }
}
