using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
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
            CurrentState = EnumAIStates.ChillWithPlayer;

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

            // Update target last known position
            PlayerControllerB? playerTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (playerTarget != null)
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

            // Set where the intern should look
            SetInternLookAt();

            // Chill
            ai.StopMoving();

            // Emotes
            npcController.MimicEmotes(ai.targetPlayer);
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

            ai.InternIdentity.Voice.PlayRandomVoiceAudio(EnumVoicesState.Chilling);
            lastVoiceState = EnumVoicesState.Chilling;
        }

        public override void PlayerHeard(Vector3 noisePosition)
        {
            // Look at origin of sound
            SetInternLookAt(noisePosition);
        }

        private void SetInternLookAt(Vector3? position = null)
        {
            if (Plugin.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
            {
                LookAtWhatPlayerPointingAt();
            }
            else
            {
                if (position.HasValue)
                {
                    npcController.OrderToLookAtPlayer(position.Value + new Vector3(0, 2.35f, 0));
                }
                else
                {
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
                }
            }
        }

        private void LookAtWhatPlayerPointingAt()
        {
            // Look where the target player is looking
            Ray interactRay = new Ray(ai.targetPlayer.gameplayCamera.transform.position, ai.targetPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                npcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                npcController.OrderToLookForward();
            }
            else
            {
                // Check if looking at a player/intern
                foreach (var hit in raycastHits)
                {
                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null
                        && player.playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        npcController.OrderToLookAtPosition(hit.point);
                        npcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        npcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                        npcController.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    npcController.OrderToLookAtPosition(hit.point);
                    npcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }
    }
}
