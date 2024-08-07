﻿using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    /// <summary>
    /// The state when the AI is close to the owner player
    /// </summary>
    /// <remarks>
    /// When close to the player, the chill state makes the intern stop moving and looking at him,
    /// check for items to grab or enemies to flee, waiting for the player to move. 
    /// </remarks>
    internal class ChillWithPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.ChillWithPlayer;
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
        public ChillWithPlayerState(AIState state) : base(state)
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

            // Target in ship, wait outside
            if (ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new PlayerInShipState(this);
                return;
            }

            VehicleController? vehicleController = ai.IsTargetPlayerInCruiserVehicle();
            if (vehicleController != null)
            {
                ai.State = new PlayerInCruiserState(this, vehicleController);
                return;
            }

            // Update target last known position
            PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
            }

            // Target too far, get close to him
            // note: not the same distance to compare in horizontal or vertical distance
            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                || SqrVerticalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // todo detect sound
                npcController.OrderToLookForward();
                ai.State = new GetCloseToPlayerState(this);
                return;
            }

            // Looking at player or forward
            PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (playerToLook != null)
            {
                npcController.OrderToLookAtPlayer(playerToLook.playerEye.position);
            }
            else
            {
                npcController.OrderToLookForward();
            }

            // Chill
            ai.StopMoving();
        }
    }
}
