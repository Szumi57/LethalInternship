using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;
using System.Collections;
using UnityEngine.Yoga;

namespace LethalInternship.AI
{
    internal class NpcController : NetworkBehaviour
    {
        public PlayerControllerB Npc { get; set; } = null!;

        private bool isCameraDisabled = true;

        private bool isWalking;
        private bool isJumping;
        private int movementHinderedPrev;
        private bool isSidling;
        private float sprintMultiplier = 1f;
        private float slopeModifier;
        private bool movingForward;
        private float crouchMeter;
        private float limpMultiplier = 0.2f;
        private Vector3 walkForce;
        private bool isFallingFromJump;
        private bool isFallingNoJump;
        private float playerSlidingTimer;
        private float slideFriction;
        private bool disabledJetpackControlsThisFrame;
        private bool teleportingThisFrame;

        private float updatePlayerLookInterval;
        private int oldConnectedPlayersAmount;
        private float updatePlayerAnimationsInterval;
        private List<int> currentAnimationStateHash = new List<int>();
        private List<int> previousAnimationStateHash = new List<int>();
        private float currentAnimationSpeed;
        private float previousAnimationSpeed;

        private RaycastHit hit;
        private Collider[] nearByPlayers = new Collider[4];

        private float targetLookRot;
        private float targetYRot;

        private float upperBodyAnimationsWeight;
        private float exhaustionEffectLerp;

        private bool wasUnderwaterLastFrame;

        public bool HasToMove { get { return lastMoveVector.y > 0f; } }
        // Orders
        private bool hasToLookAtPlayer = false;
        private bool hasToLookForward = true;
        // Fields for orders
        private Vector2 lastMoveVector;
        private Vector3 positionToUpdateTurnBodyTowardsTo;
        private Vector3 directionToUpdateTurnBodyTowardsTo;
        private Vector3 directionToUpdateTurnBodyTowardsToNormalized;
        private Transform playerEyeToLookAt = null!;
        private float floatSprint;
        private Coroutine jumpCoroutine = null!;

        public void Update()
        {
            if (Npc.IsOwner && Npc.isPlayerControlled)
            {
                if (isCameraDisabled)
                {
                    isCameraDisabled = false;
                    Npc.thisPlayerModelArms.enabled = false;
                    Npc.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                    Npc.gameObject.GetComponent<CharacterController>().enabled = true;
                    this.previousAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
                    this.currentAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
                    if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.localClientAnimatorController)
                    {
                        Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.localClientAnimatorController;
                    }

                    //if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.otherClientsAnimatorController)
                    //{
                    //    Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.otherClientsAnimatorController;
                    //}
                    //Plugin.Logger.LogDebug("----playerBodyAnimator---------");
                    //foreach (var a in Npc.playerBodyAnimator.parameters)
                    //{
                    //    Plugin.Logger.LogDebug(a.name);
                    //}
                    //Plugin.Logger.LogDebug("----localClientAnimatorController---------");
                    //foreach (var a in Npc.playersManager.localClientAnimatorController.animationClips)
                    //{
                    //    Plugin.Logger.LogDebug(a.name);
                    //}
                    //Plugin.Logger.LogDebug("-----otherClientsAnimatorController--------");
                    //foreach (var a in Npc.playersManager.otherClientsAnimatorController.animationClips)
                    //{
                    //    Plugin.Logger.LogDebug(a.name);
                    //}

                    //Animator anim = NpcController.Npc.playerBodyAnimator;
                    //Plugin.Logger.LogDebug("-------------");
                    //AnimationClip[] animationClips2 = Npc.playerBodyAnimator.runtimeAnimatorController.animationClips;
                    //Array.Sort(animationClips2, (x, y) => String.Compare(x.name, y.name));
                    //foreach (AnimationClip animClip in animationClips2)
                    //{
                    //    Plugin.Logger.LogDebug(animClip.name + ": " + animClip.length);
                    //}
                    //Plugin.Logger.LogDebug($"-----------           dead {Npc.isPlayerDead}");

                }

                if (this.HasToMove)
                {
                    if (isJumping || Npc.isCrouching)
                    {
                        this.lastMoveVector.y = Const.BASE_MAX_SPEED;
                    }
                    else
                    {
                        // Move and slow down when tight turn
                        Vector3 directionHorizontal = new Vector3(directionToUpdateTurnBodyTowardsToNormalized.x, 0f, directionToUpdateTurnBodyTowardsToNormalized.z);
                        var turnSpeed = Vector3.Dot(directionHorizontal, Npc.thisController.transform.forward);
                        
                        this.lastMoveVector.y = Mathf.Clamp(Const.BASE_MAX_SPEED * turnSpeed * 3f, Const.BASE_MIN_SPEED, Const.BASE_MAX_SPEED);
                        if (turnSpeed < 0.1f || this.lastMoveVector.y < 0.2f)
                        {
                            floatSprint = 0f;
                        }
                        //Plugin.Logger.LogDebug($"this.lastMoveVector.y {this.lastMoveVector.y}, turnSpeed {turnSpeed}");
                    }
                }
                Npc.moveInputVector.y = this.lastMoveVector.y;

                UpdateTurnBodyTowardsDirection();

                Npc.ForceTurnTowardsTarget();

                if (isWalking)
                {
                    if (Npc.moveInputVector.sqrMagnitude <= 0.001
                        || Npc.inSpecialInteractAnimation
                        && !Npc.isClimbingLadder && !Npc.inShockingMinigame)
                    {
                        isWalking = false;
                        Npc.isSprinting = false;
                        Npc.playerBodyAnimator.SetBool("Walking", false);
                        Npc.playerBodyAnimator.SetBool("Sprinting", false);
                        Npc.playerBodyAnimator.SetBool("Sideways", false);
                    }
                    else if (floatSprint > 0.3f && movementHinderedPrev <= 0 && !Npc.criticallyInjured && Npc.sprintMeter > 0.1f)
                    {
                        if (!Npc.isSprinting && Npc.sprintMeter < 0.3f)
                        {
                            if (!Npc.isExhausted)
                            {
                                Npc.isExhausted = true;
                            }
                        }
                        else
                        {
                            if (Npc.isCrouching)
                            {
                                Npc.Crouch(false);
                            }
                            Npc.isSprinting = true;
                            Npc.playerBodyAnimator.SetBool("Sprinting", true);
                        }
                    }
                    else
                    {
                        Npc.isSprinting = false;
                        if (Npc.sprintMeter < 0.1f)
                        {
                            Npc.isExhausted = true;
                        }
                        Npc.playerBodyAnimator.SetBool("Sprinting", false);
                    }
                    if (Npc.isSprinting)
                    {
                        sprintMultiplier = Mathf.Lerp(sprintMultiplier, 2.25f, Time.deltaTime * 1f);
                    }
                    else
                    {
                        sprintMultiplier = Mathf.Lerp(sprintMultiplier, 1f, 10f * Time.deltaTime);
                    }
                    if (Npc.moveInputVector.y < 0.2f && Npc.moveInputVector.y > -0.2f && !Npc.inSpecialInteractAnimation)
                    {
                        Npc.playerBodyAnimator.SetBool("Sideways", true);
                        isSidling = true;
                    }
                    else
                    {
                        Npc.playerBodyAnimator.SetBool("Sideways", false);
                        isSidling = false;
                    }
                    if (Npc.enteringSpecialAnimation)
                    {
                        Npc.playerBodyAnimator.SetFloat("animationSpeed", 1f);
                    }
                    else if (Npc.moveInputVector.y < 0.3f && Npc.moveInputVector.x < 0.3f)
                    {
                        Npc.playerBodyAnimator.SetFloat("animationSpeed", -1f * Mathf.Clamp(slopeModifier + 1f, 0.7f, 1.4f));
                    }
                    else
                    {
                        Npc.playerBodyAnimator.SetFloat("animationSpeed", 1f * Mathf.Clamp(slopeModifier + 1f, 0.7f, 1.4f));
                    }
                }
                else
                {
                    if (Npc.enteringSpecialAnimation)
                    {
                        Npc.playerBodyAnimator.SetFloat("animationSpeed", 1f);
                    }
                    else if (Npc.isClimbingLadder)
                    {
                        Npc.playerBodyAnimator.SetFloat("animationSpeed", 0f);
                    }
                    if (!Npc.isFreeCamera && Npc.moveInputVector.sqrMagnitude >= 0.001f && (!Npc.inSpecialInteractAnimation || Npc.isClimbingLadder || Npc.inShockingMinigame))
                    {
                        isWalking = true;
                        Npc.playerBodyAnimator.SetBool("Walking", true);
                    }
                }
                if (Npc.performingEmote && !CheckConditionsForEmote())
                {
                    Npc.performingEmote = false;
                    Npc.StopPerformingEmoteServerRpc();
                    Npc.timeSinceStartingEmote = 0f;
                }
                Npc.timeSinceStartingEmote += Time.deltaTime;
                Npc.playerBodyAnimator.SetBool("hinderedMovement", Npc.isMovementHindered > 0);
                if (Npc.sourcesCausingSinking == 0)
                {
                    if (Npc.isSinking)
                    {
                        Npc.isSinking = false;
                        Npc.StopSinkingServerRpc();
                    }
                }
                else
                {
                    if (Npc.isSinking)
                    {
                        Npc.GetCurrentMaterialStandingOn();
                        if (!Npc.CheckConditionsForSinkingInQuicksand())
                        {
                            Npc.isSinking = false;
                            Npc.StopSinkingServerRpc();
                        }
                    }
                    else if (!Npc.isSinking && Npc.CheckConditionsForSinkingInQuicksand())
                    {
                        Npc.isSinking = true;
                        Npc.StartSinkingServerRpc(Npc.sinkingSpeedMultiplier, Npc.statusEffectAudioIndex);
                    }
                    if (Npc.sinkingValue >= 1f)
                    {
                        Npc.KillPlayer(Vector3.zero, false, CauseOfDeath.Suffocation, 0);
                    }
                    else if (Npc.sinkingValue > 0.5f)
                    {
                        Npc.Crouch(false);
                    }
                }
                if (Npc.isCrouching)
                {
                    Npc.thisController.center = Vector3.Lerp(Npc.thisController.center, new Vector3(Npc.thisController.center.x, 0.72f, Npc.thisController.center.z), 8f * Time.deltaTime);
                    Npc.thisController.height = Mathf.Lerp(Npc.thisController.height, 1.5f, 8f * Time.deltaTime);
                }
                else
                {
                    crouchMeter = Mathf.Max(crouchMeter - Time.deltaTime * 2f, 0f);
                    Npc.thisController.center = Vector3.Lerp(Npc.thisController.center, new Vector3(Npc.thisController.center.x, 1.28f, Npc.thisController.center.z), 8f * Time.deltaTime);
                    Npc.thisController.height = Mathf.Lerp(Npc.thisController.height, 2.5f, 8f * Time.deltaTime);
                }

                if (!Npc.isClimbingLadder)
                {
                    //Vector3 localEulerAngles = Npc.transform.localEulerAngles;
                    //localEulerAngles.x = Mathf.LerpAngle(localEulerAngles.x, 0f, 15f * Time.deltaTime);
                    //localEulerAngles.z = Mathf.LerpAngle(localEulerAngles.z, 0f, 15f * Time.deltaTime);
                    //Npc.transform.localEulerAngles = localEulerAngles;
                }
                if (!Npc.inSpecialInteractAnimation || Npc.inShockingMinigame || StartOfRound.Instance.suckingPlayersOutOfShip)
                {
                    if (Npc.isFreeCamera)
                    {
                        Npc.moveInputVector = Vector2.zero;
                    }
                    CalculateGroundNormal();
                    float num3 = Npc.movementSpeed / Npc.carryWeight;
                    if (Npc.sinkingValue > 0.73f)
                    {
                        num3 = 0f;
                    }
                    else
                    {
                        if (Npc.isCrouching)
                        {
                            num3 /= 1.5f;
                        }
                        else if (Npc.criticallyInjured && !Npc.isCrouching)
                        {
                            num3 *= limpMultiplier;
                        }
                        if (Npc.isSpeedCheating)
                        {
                            Plugin.Logger.LogDebug("===================================================== speed cheat ?");
                            num3 *= 15f;
                        }
                        if (movementHinderedPrev > 0)
                        {
                            num3 /= 2f * Npc.hinderedMultiplier;
                        }
                        if (Npc.drunkness > 0f)
                        {
                            num3 *= StartOfRound.Instance.drunknessSpeedEffect.Evaluate(Npc.drunkness) / 5f + 1f;
                        }
                        if (!Npc.isCrouching && crouchMeter > 1.2f)
                        {
                            num3 *= 0.5f;
                        }
                        if (!Npc.isCrouching)
                        {
                            float num4 = Vector3.Dot(Npc.playerGroundNormal, walkForce);
                            if (num4 > 0.05f)
                            {
                                slopeModifier = Mathf.MoveTowards(slopeModifier, num4, (Npc.slopeModifierSpeed + 0.45f) * Time.deltaTime);
                            }
                            else
                            {
                                slopeModifier = Mathf.MoveTowards(slopeModifier, num4, Npc.slopeModifierSpeed / 2f * Time.deltaTime);
                            }
                            num3 = Mathf.Max(num3 * 0.8f, num3 + Npc.slopeIntensity * slopeModifier);
                        }
                    }
                    if (Npc.isTypingChat || Npc.jetpackControls && !Npc.thisController.isGrounded || StartOfRound.Instance.suckingPlayersOutOfShip)
                    {
                        Npc.moveInputVector = Vector2.zero;
                    }
                    Vector3 vector = new Vector3(0f, 0f, 0f);
                    int num5 = Physics.OverlapSphereNonAlloc(Npc.transform.position, 0.65f, nearByPlayers, StartOfRound.Instance.playersMask);
                    for (int i = 0; i < num5; i++)
                    {
                        vector += Vector3.Normalize((Npc.transform.position - nearByPlayers[i].transform.position) * 100f) * 1.2f;
                    }
                    int num6 = Physics.OverlapSphereNonAlloc(Npc.transform.position, 1.25f, nearByPlayers, 524288);
                    for (int j = 0; j < num6; j++)
                    {
                        EnemyAICollisionDetect component = nearByPlayers[j].gameObject.GetComponent<EnemyAICollisionDetect>();
                        if (component != null && component.mainScript != null && !component.mainScript.isEnemyDead && Vector3.Distance(Npc.transform.position, nearByPlayers[j].transform.position) < component.mainScript.enemyType.pushPlayerDistance)
                        {
                            vector += Vector3.Normalize((Npc.transform.position - nearByPlayers[j].transform.position) * 100f) * component.mainScript.enemyType.pushPlayerForce;
                        }
                    }
                    float num7;
                    if (isFallingFromJump || isFallingNoJump)
                    {
                        num7 = 1.33f;
                    }
                    else if (Npc.drunkness > 0.3f)
                    {
                        num7 = Mathf.Clamp(Mathf.Abs(Npc.drunkness - 2.25f), 0.3f, 2.5f);
                    }
                    else if (!Npc.isCrouching && crouchMeter > 1f)
                    {
                        num7 = 15f;
                    }
                    else if (Npc.isSprinting)
                    {
                        num7 = 5f / (Npc.carryWeight * 1.5f);
                    }
                    else
                    {
                        num7 = 10f / Npc.carryWeight;
                    }
                    walkForce = Vector3.MoveTowards(walkForce, Npc.transform.right * Npc.moveInputVector.x + Npc.transform.forward * Npc.moveInputVector.y, num7 * Time.deltaTime);
                    Vector3 vector2 = walkForce * num3 * sprintMultiplier + new Vector3(0f, Npc.fallValue, 0f) + vector;
                    vector2 += Npc.externalForces;
                    if (Npc.externalForceAutoFade.magnitude > 0.05f)
                    {
                        vector2 += Npc.externalForceAutoFade;
                        Npc.externalForceAutoFade = Vector3.Lerp(Npc.externalForceAutoFade, Vector3.zero, 2f * Time.deltaTime);
                    }
                    if (Npc.isPlayerSliding && Npc.thisController.isGrounded)
                    {
                        playerSlidingTimer += Time.deltaTime;
                        if (slideFriction > Npc.maxSlideFriction)
                        {
                            slideFriction -= 35f * Time.deltaTime;
                        }
                        vector2 = new Vector3(vector2.x + (1f - Npc.playerGroundNormal.y) * Npc.playerGroundNormal.x * (1f - slideFriction), vector2.y, vector2.z + (1f - Npc.playerGroundNormal.y) * Npc.playerGroundNormal.z * (1f - slideFriction));
                    }
                    else
                    {
                        playerSlidingTimer = 0f;
                        slideFriction = 0f;
                    }

                    //----------------------
                    // Move
                    //----------------------
                    Npc.thisController.Move(vector2 * Time.deltaTime);
                    //Plugin.Logger.LogDebug($"-----------           Move {vector2 * Time.deltaTime}, mag {(vector2 * Time.deltaTime).magnitude}");
                    if (!Npc.inSpecialInteractAnimation || Npc.inShockingMinigame)
                    {
                        if (!Npc.thisController.isGrounded)
                        {
                            if (Npc.jetpackControls && !Npc.disablingJetpackControls)
                            {
                                Npc.fallValue = Mathf.MoveTowards(Npc.fallValue, Npc.jetpackCounteractiveForce, 9f * Time.deltaTime);
                                Npc.fallValueUncapped = -8f;
                            }
                            else
                            {
                                Npc.fallValue = Mathf.Clamp(Npc.fallValue - 38f * Time.deltaTime, -150f, Npc.jumpForce);
                                if (Mathf.Abs(Npc.externalForceAutoFade.y) - Mathf.Abs(Npc.fallValue) < 5f)
                                {
                                    if (Npc.disablingJetpackControls)
                                    {
                                        Npc.fallValueUncapped -= 26f * Time.deltaTime;
                                    }
                                    else
                                    {
                                        Npc.fallValueUncapped -= 38f * Time.deltaTime;
                                    }
                                }
                            }
                            if (!isJumping && !isFallingFromJump)
                            {
                                if (!isFallingNoJump)
                                {
                                    isFallingNoJump = true;
                                    Npc.fallValue = -7f;
                                    Npc.fallValueUncapped = -7f;
                                }
                                else if (Npc.fallValue < -20f)
                                {
                                    Npc.isCrouching = false;
                                    Npc.playerBodyAnimator.SetBool("crouching", false);
                                    Npc.playerBodyAnimator.SetBool("FallNoJump", true);
                                }
                            }
                            if (Npc.fallValueUncapped < -35f)
                            {
                                Npc.takingFallDamage = true;
                            }
                        }
                        else
                        {
                            movementHinderedPrev = Npc.isMovementHindered;
                            if (!isJumping)
                            {
                                if (isFallingNoJump)
                                {
                                    isFallingNoJump = false;
                                    if (!Npc.isCrouching && Npc.fallValue < -9f)
                                    {
                                        Npc.playerBodyAnimator.SetTrigger("ShortFallLanding");
                                    }
                                    PlayerHitGroundEffects();
                                }
                                if (!isFallingFromJump)
                                {
                                    Npc.fallValue = -7f - Mathf.Clamp(12f * slopeModifier, 0f, 100f);
                                    Npc.fallValueUncapped = -7f - Mathf.Clamp(12f * slopeModifier, 0f, 100f);
                                }
                            }
                            Npc.playerBodyAnimator.SetBool("FallNoJump", false);
                        }
                    }
                    Npc.externalForces = Vector3.zero;
                    if (!teleportingThisFrame && Npc.teleportedLastFrame)
                    {
                        Npc.teleportedLastFrame = false;
                    }
                    Npc.isPlayerSliding = Vector3.Angle(Vector3.up, Npc.playerGroundNormal) >= Npc.thisController.slopeLimit;
                }
                else if (Npc.isClimbingLadder)
                {
                    Vector3 direction = Npc.thisPlayerBody.up;
                    Vector3 origin = Npc.gameplayCamera.transform.position + Npc.thisPlayerBody.up * 0.07f;
                    if ((Npc.externalForces + Npc.externalForceAutoFade).magnitude > 8f)
                    {
                        Npc.CancelSpecialTriggerAnimations();
                    }
                    Npc.externalForces = Vector3.zero;
                    Npc.externalForceAutoFade = Vector3.Lerp(Npc.externalForceAutoFade, Vector3.zero, 5f * Time.deltaTime);

                    if (directionToUpdateTurnBodyTowardsTo.y < 0f)
                    {
                        direction = -Npc.thisPlayerBody.up;
                        origin = Npc.transform.position;
                    }
                    if (!Physics.Raycast(origin, direction, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
                    {
                        Npc.thisPlayerBody.transform.position += direction * (Const.BASE_MAX_SPEED * Npc.climbSpeed * Time.deltaTime);
                    }
                    else
                    {
                        Npc.CancelSpecialTriggerAnimations();
                    }
                }
                teleportingThisFrame = false;

                // Rotations
                if (this.hasToLookAtPlayer)
                {
                    this.UpdateLookAtPlayer();
                }
                if (this.hasToLookForward)
                {
                    this.UpdateLookForward();
                }

                Npc.playerEye.position = Npc.gameplayCamera.transform.position;
                Npc.playerEye.rotation = Npc.gameplayCamera.transform.rotation;

                if (Npc.isHoldingObject && Npc.currentlyHeldObjectServer == null)
                {
                    Npc.DropAllHeldItems(true, false);
                }

                //Plugin.Logger.LogDebug($"NetworkManager.Singleton += {NetworkManager.Singleton}, Npc.IsServer {Npc.IsServer}, Npc.playersManager.connectedPlayersAmount {Npc.playersManager.connectedPlayersAmount}, oldConnectedPlayersAmount {oldConnectedPlayersAmount}");
                if (NetworkManager.Singleton != null && !Npc.IsServer || !Npc.isTestingPlayer && Npc.playersManager.connectedPlayersAmount > 0 || oldConnectedPlayersAmount >= 1)
                {
                    updatePlayerLookInterval += Time.deltaTime;
                    UpdatePlayerAnimationsToOtherClients(Npc.moveInputVector);
                }
            }
            else
            {
                /*
                if (!this.isCameraDisabled)
                {
                    this.isCameraDisabled = true;
                    Npc.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                    Npc.thisPlayerModelArms.enabled = false;
                    Npc.mapRadarDirectionIndicator.enabled = false;
                    Npc.gameObject.GetComponent<CharacterController>().enabled = false;
                    if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.otherClientsAnimatorController)
                    {
                        Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.otherClientsAnimatorController;
                    }
                    Npc.SpawnPlayerAnimation();
                    if (Npc.gameObject.GetComponent<Rigidbody>())
                    {
                        Npc.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
                    }
                }
                */
            }

            if (!Npc.isTestingPlayer && !Npc.isPlayerDead && Npc.isPlayerControlled)
            {
                //if (!Npc.disableSyncInAnimation)
                //{
                //    if (Npc.snapToServerPosition)
                //    {
                //        Npc.transform.localPosition = Vector3.Lerp(Npc.transform.localPosition, Npc.serverPlayerPosition, 16f * Time.deltaTime);
                //    }
                //    else
                //    {
                //        float num10 = 8f;
                //        if (Npc.jetpackControls)
                //        {
                //            num10 = 15f;
                //        }
                //        float num11 = Mathf.Clamp(num10 * Vector3.Distance(Npc.transform.localPosition, Npc.serverPlayerPosition), 0.9f, 300f);
                //        Npc.transform.localPosition = Vector3.MoveTowards(Npc.transform.localPosition, Npc.serverPlayerPosition, num11 * Time.deltaTime);
                //    }
                //}
                //if (Npc.jetpackControls || Npc.disablingJetpackControls || Npc.isClimbingLadder)
                //{
                //    if (!Npc.disableSyncInAnimation)
                //    {
                //        RoundManager.Instance.tempTransform.rotation = Quaternion.Euler(Npc.syncFullRotation);
                //        Npc.transform.rotation = Quaternion.Lerp(Quaternion.Euler(Npc.transform.eulerAngles), Quaternion.Euler(Npc.syncFullRotation), 8f * Time.deltaTime);
                //    }
                //}
                //else
                //{
                //    Npc.syncFullRotation = Npc.transform.eulerAngles;
                //    if (!Npc.disableSyncInAnimation)
                //    {
                //        Npc.transform.eulerAngles = new Vector3(Npc.transform.eulerAngles.x, Mathf.LerpAngle(Npc.transform.eulerAngles.y, this.targetYRot, 14f * Time.deltaTime), Npc.transform.eulerAngles.z);
                //    }
                //    if (!Npc.inSpecialInteractAnimation && !Npc.disableSyncInAnimation)
                //    {
                //        Vector3 localEulerAngles2 = Npc.transform.localEulerAngles;
                //        localEulerAngles2.x = Mathf.LerpAngle(localEulerAngles2.x, 0f, 25f * Time.deltaTime);
                //        localEulerAngles2.z = Mathf.LerpAngle(localEulerAngles2.z, 0f, 25f * Time.deltaTime);
                //        Npc.transform.localEulerAngles = localEulerAngles2;
                //    }
                //}
                //Npc.playerEye.position = Npc.gameplayCamera.transform.position;
                //Npc.playerEye.localEulerAngles = new Vector3(this.targetLookRot, 0f, 0f);
                //Npc.playerEye.eulerAngles = new Vector3(Npc.playerEye.eulerAngles.x, this.targetYRot, Npc.playerEye.eulerAngles.z);
            }
            else if ((Npc.isPlayerDead || !Npc.isPlayerControlled) && Npc.setPositionOfDeadPlayer)
            {
                Npc.transform.position = Npc.playersManager.notSpawnedPosition.position;
            }
            if (Npc.isInGameOverAnimation > 0f && Npc.deadBody != null && Npc.deadBody.gameObject.activeSelf)
            {
                Npc.isInGameOverAnimation -= Time.deltaTime;
            }
            else if (!Npc.hasBegunSpectating)
            {
                if (Npc.deadBody != null)
                {
                    Debug.Log(Npc.deadBody.gameObject.activeSelf);
                }
                Npc.isInGameOverAnimation = 0f;
                Npc.hasBegunSpectating = true;
            }

            Npc.timeSincePlayerMoving += Time.deltaTime;
            Npc.timeSinceMakingLoudNoise += Time.deltaTime;
            if (!Npc.inSpecialInteractAnimation)
            {
                if (Npc.playingQuickSpecialAnimation)
                {
                    Npc.specialAnimationWeight = 1f;
                }
                else
                {
                    Npc.specialAnimationWeight = Mathf.Lerp(Npc.specialAnimationWeight, 0f, Time.deltaTime * 12f);
                }
                if (!Npc.localArmsMatchCamera)
                {
                    Npc.localArmsTransform.position = Npc.playerModelArmsMetarig.position + Npc.playerModelArmsMetarig.forward * -0.445f;
                    Npc.playerModelArmsMetarig.rotation = Quaternion.Lerp(Npc.playerModelArmsMetarig.rotation, Npc.localArmsRotationTarget.rotation, 15f * Time.deltaTime);
                }
            }
            else
            {
                Npc.specialAnimationWeight = Mathf.Lerp(Npc.specialAnimationWeight, 1f, Time.deltaTime * 20f);
                Npc.playerModelArmsMetarig.localEulerAngles = new Vector3(-90f, 0f, 0f);
            }

            if (Npc.doingUpperBodyEmote > 0f)
            {
                Npc.doingUpperBodyEmote -= Time.deltaTime;
            }

            if (Npc.performingEmote)
            {
                Npc.emoteLayerWeight = Mathf.Lerp(Npc.emoteLayerWeight, 1f, 10f * Time.deltaTime);
            }
            else
            {
                Npc.emoteLayerWeight = Mathf.Lerp(Npc.emoteLayerWeight, 0f, 10f * Time.deltaTime);
            }
            Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("EmotesNoArms"), Npc.emoteLayerWeight);
            Npc.meshContainer.position = Vector3.Lerp(Npc.transform.position, Npc.transform.position - Vector3.up * 2.8f, StartOfRound.Instance.playerSinkingCurve.Evaluate(Npc.sinkingValue));
            if (Npc.isSinking && !Npc.inSpecialInteractAnimation && Npc.inAnimationWithEnemy == null)
            {
                Npc.sinkingValue = Mathf.Clamp(Npc.sinkingValue + Time.deltaTime * Npc.sinkingSpeedMultiplier, 0f, 1f);
            }
            else
            {
                Npc.sinkingValue = Mathf.Clamp(Npc.sinkingValue - Time.deltaTime * 0.75f, 0f, 1f);
            }
            if (Npc.sinkingValue > 0.73f || Npc.isUnderwater)
            {
                if (!this.wasUnderwaterLastFrame)
                {
                    this.wasUnderwaterLastFrame = true;
                    if (!Npc.IsOwner)
                    {
                        Npc.waterBubblesAudio.Play();
                    }
                }
                Npc.voiceMuffledByEnemy = true;
                if (Npc.IsOwner && Npc.sinkingValue > 0.73f)
                {
                    HUDManager.Instance.sinkingCoveredFace = true;
                }
            }
            else if (Npc.IsOwner)
            {
                HUDManager.Instance.sinkingCoveredFace = false;
            }
            else if (this.wasUnderwaterLastFrame)
            {
                Npc.waterBubblesAudio.Stop();
            }
            else
            {
                Npc.statusEffectAudio.volume = Mathf.Lerp(Npc.statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
            }
            if (Npc.activeAudioReverbFilter == null)
            {
                Npc.activeAudioReverbFilter = Npc.activeAudioListener.GetComponent<AudioReverbFilter>();
                Npc.activeAudioReverbFilter.enabled = true;
            }
            if (Npc.reverbPreset != null && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null
                && ((GameNetworkManager.Instance.localPlayerController == this.Npc
                && (!Npc.isPlayerDead || StartOfRound.Instance.overrideSpectateCamera)) || (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this.Npc && !StartOfRound.Instance.overrideSpectateCamera)))
            {
                Npc.activeAudioReverbFilter.dryLevel = Mathf.Lerp(Npc.activeAudioReverbFilter.dryLevel, Npc.reverbPreset.dryLevel, 15f * Time.deltaTime);
                Npc.activeAudioReverbFilter.roomLF = Mathf.Lerp(Npc.activeAudioReverbFilter.roomLF, Npc.reverbPreset.lowFreq, 15f * Time.deltaTime);
                Npc.activeAudioReverbFilter.roomLF = Mathf.Lerp(Npc.activeAudioReverbFilter.roomHF, Npc.reverbPreset.highFreq, 15f * Time.deltaTime);
                Npc.activeAudioReverbFilter.decayTime = Mathf.Lerp(Npc.activeAudioReverbFilter.decayTime, Npc.reverbPreset.decayTime, 15f * Time.deltaTime);
                Npc.activeAudioReverbFilter.room = Mathf.Lerp(Npc.activeAudioReverbFilter.room, Npc.reverbPreset.room, 15f * Time.deltaTime);
            }

            if (Npc.isHoldingObject || Npc.isGrabbingObjectAnimation || Npc.inShockingMinigame)
            {
                this.upperBodyAnimationsWeight = Mathf.Lerp(this.upperBodyAnimationsWeight, 1f, 25f * Time.deltaTime);
                Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), this.upperBodyAnimationsWeight);
                if (Npc.twoHandedAnimation || Npc.inShockingMinigame)
                {
                    Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), this.upperBodyAnimationsWeight);
                }
                else
                {
                    Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), Mathf.Abs(this.upperBodyAnimationsWeight - 1f));
                }
            }
            else
            {
                this.upperBodyAnimationsWeight = Mathf.Lerp(this.upperBodyAnimationsWeight, 0f, 25f * Time.deltaTime);
                Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), this.upperBodyAnimationsWeight);
                Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), this.upperBodyAnimationsWeight);
            }
            Npc.playerBodyAnimator.SetLayerWeight(Npc.playerBodyAnimator.GetLayerIndex("SpecialAnimations"), Npc.specialAnimationWeight);

            if (Npc.isExhausted)
            {
                this.exhaustionEffectLerp = Mathf.Lerp(this.exhaustionEffectLerp, 1f, 10f * Time.deltaTime);
            }
            else
            {
                this.exhaustionEffectLerp = Mathf.Lerp(this.exhaustionEffectLerp, 0f, 10f * Time.deltaTime);
            }
            Npc.playerBodyAnimator.SetFloat("tiredAmount", this.exhaustionEffectLerp);
        }

        public void OrderToMove()
        {
            if (this.lastMoveVector.y < Const.BASE_MIN_SPEED)
            {
                this.lastMoveVector = new Vector2(0f, Const.BASE_MAX_SPEED);
            }
        }

        public void OrderForceToMove()
        {
            this.lastMoveVector = new Vector2(0f, Const.BASE_MAX_SPEED);
        }

        public void OrderToStopMoving()
        {
            this.lastMoveVector = Vector2.zero;
            floatSprint = 0f;
        }

        public void OrderToSprint()
        {
            if (Npc.inSpecialInteractAnimation || !Npc.thisController.isGrounded || Npc.isClimbingLadder)
            {
                return;
            }
            if (this.isJumping)
            {
                return;
            }
            if (Npc.isSprinting)
            {
                return;
            }

            floatSprint = 1f;
        }
        public void OrderToStopSprint()
        {
            if (Npc.inSpecialInteractAnimation || !Npc.thisController.isGrounded || Npc.isClimbingLadder)
            {
                return;
            }
            if (this.isJumping)
            {
                return;
            }
            if (Npc.isSprinting)
            {
                return;
            }

            floatSprint = 0f;
        }

        public void OrderToToggleCrouch()
        {
            if (Npc.inSpecialInteractAnimation || !Npc.thisController.isGrounded || Npc.isClimbingLadder)
            {
                return;
            }
            if (this.isJumping)
            {
                return;
            }
            if (Npc.isSprinting)
            {
                return;
            }
            this.crouchMeter = Mathf.Min(this.crouchMeter + 0.3f, 1.3f);
            Npc.Crouch(!Npc.isCrouching);
        }

        public void OrderToJump()
        {
            if (Npc.inSpecialInteractAnimation)
            {
                return;
            }
            if (Npc.isMovementHindered > 0 && !Npc.isUnderwater)
            {
                return;
            }
            if (Npc.isExhausted)
            {
                return;
            }
            if ((Npc.thisController.isGrounded || (!this.isJumping && this.IsPlayerNearGround()))
                && !this.isJumping
                && (!Npc.isPlayerSliding || this.playerSlidingTimer > 2.5f)
                && !Npc.isCrouching)
            {
                this.playerSlidingTimer = 0f;
                this.isJumping = true;
                Npc.sprintMeter = Mathf.Clamp(Npc.sprintMeter - 0.08f, 0f, 1f);
                StartOfRound.Instance.PlayerJumpEvent.Invoke(this.Npc);
                this.PlayJumpAudio();
                if (this.jumpCoroutine != null)
                {
                    base.StopCoroutine(this.jumpCoroutine);
                }
                this.jumpCoroutine = base.StartCoroutine(this.PlayerJump());
                if (StartOfRound.Instance.connectedPlayersAmount != 0)
                {
                    Npc.PlayerJumpedServerRpc();
                }
            }
        }
        private bool IsPlayerNearGround()
        {
            Ray interactRay = new Ray(base.transform.position, Vector3.down);
            return Physics.Raycast(interactRay, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore);
        }

        private void PlayJumpAudio()
        {
            if (StartOfRound.Instance.unlockablesList.unlockables[Npc.currentSuitID].jumpAudio != null)
            {
                Npc.movementAudio.PlayOneShot(StartOfRound.Instance.unlockablesList.unlockables[Npc.currentSuitID].jumpAudio);
                return;
            }
            Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
        }

        private IEnumerator PlayerJump()
        {
            Npc.playerBodyAnimator.SetBool("Jumping", true);
            yield return new WaitForSeconds(0.15f);
            Npc.fallValue = Npc.jumpForce;
            Npc.fallValueUncapped = Npc.jumpForce;
            yield return new WaitForSeconds(0.1f);
            this.isJumping = false;
            this.isFallingFromJump = true;
            yield return new WaitUntil(() => Npc.thisController.isGrounded);
            Npc.playerBodyAnimator.SetBool("Jumping", false);
            this.isFallingFromJump = false;
            this.PlayerHitGroundEffects();
            this.jumpCoroutine = null!;
            yield break;
        }

        public void SetTurnBodyTowardsDirection(Vector3 positionDirection)
        {
            positionToUpdateTurnBodyTowardsTo = positionDirection;
            directionToUpdateTurnBodyTowardsTo = positionToUpdateTurnBodyTowardsTo - Npc.thisController.transform.position;
            directionToUpdateTurnBodyTowardsToNormalized = directionToUpdateTurnBodyTowardsTo.normalized;
        }
        private void UpdateTurnBodyTowardsDirection()
        {
            if (Npc.inSpecialInteractAnimation)
            {
                return;
            }

            Vector3 direction = directionToUpdateTurnBodyTowardsTo;
            if (DirectionNotZero(direction.x) || DirectionNotZero(direction.z))
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                Npc.thisPlayerBody.rotation = Quaternion.Lerp(Npc.thisPlayerBody.rotation, targetRotation, Const.BODY_TURNSPEED * Time.deltaTime);
            }
        }

        public void OrderToLookAtPlayer(PlayerControllerB player)
        {
            this.playerEyeToLookAt = player.playerEye;
            this.hasToLookAtPlayer = true;
            this.hasToLookForward = !this.hasToLookAtPlayer;
        }
        private void UpdateLookAtPlayer()
        {
            if (Npc.inSpecialInteractAnimation)
            {
                return;
            }

            Vector3 direction = playerEyeToLookAt.position - Npc.gameplayCamera.transform.position;
            if (DirectionNotZero(direction.x) || DirectionNotZero(direction.y) || DirectionNotZero(direction.z))
            {
                Quaternion cameraRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));
                Npc.gameplayCamera.transform.rotation = Quaternion.Lerp(Npc.gameplayCamera.transform.rotation, cameraRotation, Const.CAMERA_TURNSPEED * Time.deltaTime);
                if (180f - Vector3.Angle(playerEyeToLookAt.forward, Npc.thisPlayerBody.transform.forward) > Const.INTERN_FOV - 5f)
                {
                    SetTurnBodyTowardsDirection(playerEyeToLookAt.position);
                }
            }
        }

        public void OrderToLookForward()
        {
            this.hasToLookForward = true;
            this.hasToLookAtPlayer = !this.hasToLookForward;
        }
        private void UpdateLookForward()
        {
            if (Npc.inSpecialInteractAnimation)
            {
                return;
            }
            Npc.gameplayCamera.transform.rotation = Quaternion.Lerp(Npc.gameplayCamera.transform.rotation, Npc.thisPlayerBody.rotation, Const.CAMERA_TURNSPEED * Time.deltaTime);
        }

        private bool DirectionNotZero(float direction)
        {
            return direction < -Const.EPSILON || Const.EPSILON < direction;
        }

        private bool CheckConditionsForEmote()
        {
            return !Npc.inSpecialInteractAnimation
                && !Npc.isPlayerDead
                && !isJumping
                && !isWalking
                && !Npc.isCrouching
                && !Npc.isClimbingLadder
                && !Npc.isGrabbingObjectAnimation;
        }

        private void CalculateGroundNormal()
        {
            if (Physics.Raycast(Npc.transform.position + Vector3.up * 0.2f, -Vector3.up, out hit, 6f, 268438273, QueryTriggerInteraction.Ignore))
            {
                Npc.playerGroundNormal = hit.normal;
                return;
            }
            Npc.playerGroundNormal = Vector3.up;
        }

        private void PlayerHitGroundEffects()
        {
            Npc.GetCurrentMaterialStandingOn();
            if (Npc.fallValue < -9f)
            {
                if (Npc.fallValue < -16f)
                {
                    Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
                    WalkieTalkie.TransmitOneShotAudio(Npc.movementAudio, StartOfRound.Instance.playerHitGroundHard, 1f);
                }
                else if (Npc.fallValue < -2f)
                {
                    Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                }
                Npc.LandFromJumpServerRpc(Npc.fallValue < -16f);
            }
            float num = Npc.fallValueUncapped;
            if (disabledJetpackControlsThisFrame && Vector3.Angle(Npc.transform.up, Vector3.up) > 80f)
            {
                num -= 10f;
            }
            if (Npc.takingFallDamage && !Npc.isSpeedCheating)
            {
                if (Npc.fallValueUncapped < -48.5f)
                {
                    Npc.DamagePlayer(100, true, true, CauseOfDeath.Gravity, 0, false, default);
                }
                else if (Npc.fallValueUncapped < -45f)
                {
                    Npc.DamagePlayer(80, true, true, CauseOfDeath.Gravity, 0, false, default);
                }
                else if (Npc.fallValueUncapped < -40f)
                {
                    Npc.DamagePlayer(50, true, true, CauseOfDeath.Gravity, 0, false, default);
                }
                else if (Npc.fallValue < -38f)
                {
                    Npc.DamagePlayer(30, true, true, CauseOfDeath.Gravity, 0, false, default);
                }
            }
            if (Npc.fallValue < -16f)
            {
                RoundManager.Instance.PlayAudibleNoise(Npc.transform.position, 7f, 0.5f, 0, false, 0);
            }
        }

        private void UpdatePlayerAnimationsToOtherClients(Vector2 moveInputVector)
        {
            updatePlayerAnimationsInterval += Time.deltaTime;
            if (Npc.inSpecialInteractAnimation || updatePlayerAnimationsInterval > 0.14f)
            {
                updatePlayerAnimationsInterval = 0f;
                currentAnimationSpeed = Npc.playerBodyAnimator.GetFloat("animationSpeed");
                for (int i = 0; i < Npc.playerBodyAnimator.layerCount; i++)
                {
                    currentAnimationStateHash[i] = Npc.playerBodyAnimator.GetCurrentAnimatorStateInfo(i).fullPathHash;
                    if (previousAnimationStateHash[i] != currentAnimationStateHash[i])
                    {
                        previousAnimationStateHash[i] = currentAnimationStateHash[i];
                        previousAnimationSpeed = currentAnimationSpeed;
                        UpdatePlayerAnimationServerRpc(currentAnimationStateHash[i], currentAnimationSpeed);
                        return;
                    }
                }
                if (previousAnimationSpeed != currentAnimationSpeed)
                {
                    previousAnimationSpeed = currentAnimationSpeed;
                    UpdatePlayerAnimationServerRpc(0, currentAnimationSpeed);
                }
            }
        }

        [ServerRpc]
        private void UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)
        {
            NetworkManager networkManager = Npc.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
            {
                if (OwnerClientId != networkManager.LocalClientId)
                {
                    if (networkManager.LogLevel <= LogLevel.Normal)
                    {
                        Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                    }
                    return;
                }
                ServerRpcParams serverRpcParams = new ServerRpcParams();
                FastBufferWriter writer = __beginSendServerRpc(3473255830U, serverRpcParams, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(writer, animationState);
                writer.WriteValueSafe(animationSpeed, default);
                __endSendServerRpc(ref writer, 3473255830U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            try
            {
                UpdatePlayerAnimationClientRpc(animationState, animationSpeed);
            }
            catch (Exception arg)
            {
                Debug.Log(string.Format("Client rpc parameters were likely not correct, so an RPC was skipped: {0}", arg));
            }
        }

        [ClientRpc]
        private void UpdatePlayerAnimationClientRpc(int animationState, float animationSpeed)
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter writer = __beginSendClientRpc(3386813972U, clientRpcParams, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(writer, animationState);
                writer.WriteValueSafe(animationSpeed, default);
                __endSendClientRpc(ref writer, 3386813972U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            if (IsOwner)
            {
                return;
            }
            if (Npc.playerBodyAnimator.GetFloat("animationSpeed") != animationSpeed)
            {
                Npc.playerBodyAnimator.SetFloat("animationSpeed", animationSpeed);
            }
            if (animationState != 0 && Npc.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash != animationState)
            {
                for (int i = 0; i < Npc.playerBodyAnimator.layerCount; i++)
                {
                    if (Npc.playerBodyAnimator.HasState(i, animationState))
                    {
                        Npc.playerBodyAnimator.CrossFadeInFixedTime(animationState, 0.1f);
                        return;
                    }
                }
            }
        }
    }
}