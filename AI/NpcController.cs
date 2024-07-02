using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Patches.NpcPatches;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace LethalInternship.AI
{
    internal class NpcController
    {
        public PlayerControllerB Npc { get; set; } = null!;

        private InternAI? InternAIOwner
        {
            get
            {
                if (_internAIOwner == null)
                {
                    _internAIOwner = InternManager.Instance.GetInternAIIfLocalIsOwner((int)Npc.playerClientId);
                }
                return _internAIOwner;
            }
        }
        private InternAI? _internAIOwner;

        private int movementHinderedPrev;
        private float sprintMultiplier = 1f;
        private float slopeModifier;
        private float limpMultiplier = 0.2f;
        private Vector3 walkForce;
        private bool isFallingFromJump;
        private bool isFallingNoJump;
        private float slideFriction;
        private int oldConnectedPlayersAmount;

        private Collider[] nearByPlayers = new Collider[4];

        private float upperBodyAnimationsWeight;
        private float exhaustionEffectLerp;

        private bool wasUnderwaterLastFrame;
        private float drowningTimer = 1f;
        private bool disabledJetpackControlsThisFrame;

        public bool HasToMove { get { return lastMoveVector.y > 0f; } }
        private EnumObjectsLookingAt enumObjectsLookingAt;

        // Public variables to pass to patch
        public bool IsCameraDisabled;
        public bool IsJumping;
        public float CrouchMeter;
        public bool IsWalking;
        public float PlayerSlidingTimer;
        public bool DisabledJetpackControlsThisFrame;
        public bool StartedJetpackControls;
        public float UpdatePlayerAnimationsInterval;
        public float CurrentAnimationSpeed;
        public float PreviousAnimationSpeed;
        public List<int> CurrentAnimationStateHash = new List<int>();
        public List<int> PreviousAnimationStateHash = new List<int>();
        public Vector3 RightArmProceduralTargetBasePosition;
        public int PreviousAnimationState;
        public float TimeSinceTakingGravityDamage;
        public bool TeleportingThisFrame;
        public float PreviousFrameDeltaTime;
        public float CameraUp;

        public bool UpdatePositionForNewlyJoinedClient;
        public bool GrabbedObjectValidated;
        public float UpdatePlayerLookInterval;
        public int PlayerMask;

        private int oldSentIntEnumObjectsLookingAt;
        private Vector3 oldSentDirectionToUpdateTurnBodyTowardsTo;
        private Vector3 oldSentPositionPlayerEyeToLookAt;
        private Vector3 oldSentPositionToLookAt;

        private Vector3 directionToUpdateTurnBodyTowardsTo;
        private Vector3 directionToUpdateTurnBodyTowardsToNormalized;
        private Vector3 positionPlayerEyeToLookAt;
        private Vector3 positionToLookAt;


        private Vector2 lastMoveVector;
        private float floatSprint;
        private bool goDownLadder;

        private RaycastHit hit;

        public NpcController(PlayerControllerB npc)
        {
            this.Npc = npc;
        }

        public void Awake()
        {
            Plugin.Logger.LogDebug("Awake intern controller.");
            Npc.isHostPlayerObject = false;
            //Npc.playerActions = new PlayerActions();
            this.PreviousAnimationState = 0;
            Npc.serverPlayerPosition = Npc.transform.position;
            Npc.gameplayCamera.enabled = false;
            Npc.visorCamera.enabled = false;
            Npc.thisPlayerModel.enabled = true;
            Npc.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
            Npc.thisPlayerModelArms.enabled = false;
            //PreviousAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
            //CurrentAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
            //if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.otherClientsAnimatorController)
            //{
            //    Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.otherClientsAnimatorController;
            //}
            this.IsCameraDisabled = true;
            Npc.sprintMeter = 1f;
            Npc.ItemSlots = new GrabbableObject[1];
            RightArmProceduralTargetBasePosition = Npc.rightArmProceduralTarget.localPosition;
            Npc.usernameBillboardText.text = Npc.playerUsername;
            Npc.previousElevatorPosition = Npc.playersManager.elevatorTransform.position;
            if (Npc.gameObject.GetComponent<Rigidbody>())
            {
                Npc.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
            }
            Npc.gameObject.GetComponent<CharacterController>().enabled = false;
            //Npc.syncFullRotation = Npc.transform.eulerAngles;
        }

        public void Update()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;

            if (InternManager.Instance.IsPlayerInternOwnerLocal(Npc) && Npc.isPlayerControlled)
            {
                if (IsCameraDisabled)
                {
                    IsCameraDisabled = false;
                    Npc.gameplayCamera.enabled = false;
                    Npc.visorCamera.enabled = false;
                    Npc.thisPlayerModelArms.enabled = false;
                    Npc.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                    Npc.mapRadarDirectionIndicator.enabled = false;
                    PreviousAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
                    CurrentAnimationStateHash = new List<int>(new int[Npc.playerBodyAnimator.layerCount]);
                    if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.localClientAnimatorController)
                    {
                        Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.localClientAnimatorController;
                    }
                }

                if (Npc.thisController.enabled != !Npc.inSpecialInteractAnimation)
                {
                    Npc.thisController.enabled = !Npc.inSpecialInteractAnimation;
                }

                Npc.rightArmProceduralRig.weight = Mathf.Lerp(Npc.rightArmProceduralRig.weight, 0f, 25f * Time.deltaTime);

                if (this.HasToMove)
                {
                    if (IsJumping || Npc.isCrouching)
                    {
                        this.lastMoveVector.y = Const.BASE_MAX_SPEED;
                    }
                    else
                    {
                        // Move and slow down when tight turn
                        Vector3 directionHorizontal = new Vector3(directionToUpdateTurnBodyTowardsToNormalized.x, 0f, directionToUpdateTurnBodyTowardsToNormalized.z);
                        var turnSpeed = Vector3.Dot(directionHorizontal, Npc.thisController.transform.forward);

                        this.lastMoveVector.y = Mathf.Clamp(Const.BASE_MAX_SPEED * turnSpeed * 2.5f, Const.BASE_MIN_SPEED, Const.BASE_MAX_SPEED);
                        if (turnSpeed < 0.15f || this.lastMoveVector.y < 0.2f)
                        {
                            floatSprint = 0f;
                        }
                        //Plugin.Logger.LogDebug($"this.lastMoveVector.y {this.lastMoveVector.y}, turnSpeed {turnSpeed}");
                    }
                }
                Npc.moveInputVector.y = this.lastMoveVector.y;
                //if (floatSprint > 0f)
                //{
                //    Plugin.Logger.LogDebug($"-----------           Move {Npc.moveInputVector.y}");
                //}
                UpdateTurnBodyTowardsDirection();

                Npc.ForceTurnTowardsTarget();

                SetFaceUnderwaterFilters();

                if (IsWalking)
                {
                    if (Npc.moveInputVector.sqrMagnitude <= 0.001
                        || Npc.inSpecialInteractAnimation
                        && !Npc.isClimbingLadder && !Npc.inShockingMinigame)
                    {
                        IsWalking = false;
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
                    }
                    else
                    {
                        Npc.playerBodyAnimator.SetBool("Sideways", false);
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
                        IsWalking = true;
                        Npc.playerBodyAnimator.SetBool("Walking", true);
                    }
                }
                if (Npc.performingEmote && !PlayerControllerBPatch.CheckConditionsForEmote_ReversePatch(this.Npc))
                {
                    Npc.performingEmote = false;
                    this.InternAIOwner?.SyncStopPerformingEmote();
                    Npc.timeSinceStartingEmote = 0f;
                }
                Npc.timeSinceStartingEmote += Time.deltaTime;
                Npc.playerBodyAnimator.SetBool("hinderedMovement", Npc.isMovementHindered > 0);
                if (Npc.sourcesCausingSinking == 0)
                {
                    if (Npc.isSinking)
                    {
                        Npc.isSinking = false;
                        this.InternAIOwner?.SyncChangeSinkingState(false);
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
                            this.InternAIOwner?.SyncChangeSinkingState(false);
                        }
                    }
                    else if (!Npc.isSinking && Npc.CheckConditionsForSinkingInQuicksand())
                    {
                        Npc.isSinking = true;
                        this.InternAIOwner?.SyncChangeSinkingState(true, Npc.sinkingSpeedMultiplier, Npc.statusEffectAudioIndex);
                    }
                    if (Npc.sinkingValue >= 1f)
                    {
                        Plugin.Logger.LogDebug($"SyncKillIntern from sinkingValue for LOCAL client #{Npc.NetworkManager.LocalClientId}, intern object: Intern #{Npc.playerClientId}");
                        this.InternAIOwner?.SyncKillIntern(Vector3.zero, false, CauseOfDeath.Suffocation, 0);
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
                    CrouchMeter = Mathf.Max(CrouchMeter - Time.deltaTime * 2f, 0f);
                    Npc.thisController.center = Vector3.Lerp(Npc.thisController.center, new Vector3(Npc.thisController.center.x, 1.28f, Npc.thisController.center.z), 8f * Time.deltaTime);
                    Npc.thisController.height = Mathf.Lerp(Npc.thisController.height, 2.5f, 8f * Time.deltaTime);
                }

                if (this.disabledJetpackControlsThisFrame)
                {
                    this.disabledJetpackControlsThisFrame = false;
                }
                if (Npc.jetpackControls)
                {
                    if (Npc.disablingJetpackControls && Npc.thisController.isGrounded)
                    {
                        this.disabledJetpackControlsThisFrame = true;
                        this.InternAIOwner?.SyncDisableJetpackMode();
                    }
                    else if (!Npc.thisController.isGrounded)
                    {
                        if (!this.StartedJetpackControls)
                        {
                            this.StartedJetpackControls = true;
                            Npc.jetpackTurnCompass.rotation = Npc.transform.rotation;
                        }
                        Npc.thisController.radius = Mathf.Lerp(Npc.thisController.radius, 1.25f, 10f * Time.deltaTime);
                        Quaternion rotation = Npc.jetpackTurnCompass.rotation;
                        Npc.jetpackTurnCompass.Rotate(new Vector3(0f, 0f, -Npc.moveInputVector.x) * (180f * Time.deltaTime), Space.Self);
                        if (Npc.maxJetpackAngle != -1f && Vector3.Angle(Npc.jetpackTurnCompass.up, Vector3.up) > Npc.maxJetpackAngle)
                        {
                            Npc.jetpackTurnCompass.rotation = rotation;
                        }
                        rotation = Npc.jetpackTurnCompass.rotation;
                        Npc.jetpackTurnCompass.Rotate(new Vector3(Npc.moveInputVector.y, 0f, 0f) * (180f * Time.deltaTime), Space.Self);
                        if (Npc.maxJetpackAngle != -1f && Vector3.Angle(Npc.jetpackTurnCompass.up, Vector3.up) > Npc.maxJetpackAngle)
                        {
                            Npc.jetpackTurnCompass.rotation = rotation;
                        }
                        if (Npc.jetpackRandomIntensity != -1f)
                        {
                            rotation = Npc.jetpackTurnCompass.rotation;
                            Vector3 a2 = new Vector3(
                                Mathf.Clamp(
                                    Random.Range(-Npc.jetpackRandomIntensity, Npc.jetpackRandomIntensity),
                                -Npc.maxJetpackAngle, Npc.maxJetpackAngle),
                                Mathf.Clamp(
                                    Random.Range(-Npc.jetpackRandomIntensity, Npc.jetpackRandomIntensity), -Npc.maxJetpackAngle, Npc.maxJetpackAngle),
                                Mathf.Clamp(Random.Range(-Npc.jetpackRandomIntensity, Npc.jetpackRandomIntensity), -Npc.maxJetpackAngle, Npc.maxJetpackAngle));
                            Npc.jetpackTurnCompass.Rotate(a2 * Time.deltaTime, Space.Self);
                            if (Npc.maxJetpackAngle != -1f && Vector3.Angle(Npc.jetpackTurnCompass.up, Vector3.up) > Npc.maxJetpackAngle)
                            {
                                Npc.jetpackTurnCompass.rotation = rotation;
                            }
                        }
                        Npc.transform.rotation = Quaternion.Slerp(Npc.transform.rotation, Npc.jetpackTurnCompass.rotation, 8f * Time.deltaTime);
                    }
                }
                else if (!Npc.isClimbingLadder)
                {
                    //Vector3 localEulerAngles = Npc.transform.localEulerAngles;
                    //localEulerAngles.x = Mathf.LerpAngle(localEulerAngles.x, 0f, 15f * Time.deltaTime);
                    //localEulerAngles.z = Mathf.LerpAngle(localEulerAngles.z, 0f, 15f * Time.deltaTime);
                    //Npc.transform.localEulerAngles = localEulerAngles;
                }
                if (!Npc.inSpecialInteractAnimation || Npc.inShockingMinigame || instanceSOR.suckingPlayersOutOfShip)
                {
                    if (Npc.isFreeCamera)
                    {
                        Npc.moveInputVector = Vector2.zero;
                    }
                    PlayerControllerBPatch.CalculateGroundNormal_ReversePatch(this.Npc);
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
                            num3 *= instanceSOR.drunknessSpeedEffect.Evaluate(Npc.drunkness) / 5f + 1f;
                        }
                        if (!Npc.isCrouching && CrouchMeter > 1.2f)
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
                    if (Npc.isTypingChat || Npc.jetpackControls && !Npc.thisController.isGrounded || instanceSOR.suckingPlayersOutOfShip)
                    {
                        Npc.moveInputVector = Vector2.zero;
                    }
                    Vector3 vector = new Vector3(0f, 0f, 0f);
                    int num5 = Physics.OverlapSphereNonAlloc(Npc.transform.position, 0.65f, nearByPlayers, instanceSOR.playersMask);
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
                    else if (!Npc.isCrouching && CrouchMeter > 1f)
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
                        PlayerSlidingTimer += Time.deltaTime;
                        if (slideFriction > Npc.maxSlideFriction)
                        {
                            slideFriction -= 35f * Time.deltaTime;
                        }
                        vector2 = new Vector3(vector2.x + (1f - Npc.playerGroundNormal.y) * Npc.playerGroundNormal.x * (1f - slideFriction), vector2.y, vector2.z + (1f - Npc.playerGroundNormal.y) * Npc.playerGroundNormal.z * (1f - slideFriction));
                    }
                    else
                    {
                        PlayerSlidingTimer = 0f;
                        slideFriction = 0f;
                    }

                    //----------------------
                    // Move
                    //----------------------
                    Npc.thisController.Move(vector2 * Time.deltaTime);
                    //if(floatSprint >  0f)
                    //{
                    //Plugin.Logger.LogDebug($"-----------           Move {vector2 * Time.deltaTime}, mag {(vector2 * Time.deltaTime).magnitude}");

                    //}
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
                            if (!IsJumping && !isFallingFromJump)
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
                            if (!IsJumping)
                            {
                                if (isFallingNoJump)
                                {
                                    isFallingNoJump = false;
                                    if (!Npc.isCrouching && Npc.fallValue < -9f)
                                    {
                                        Npc.playerBodyAnimator.SetTrigger("ShortFallLanding");
                                    }
                                    PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch(this.Npc);
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
                    if (!TeleportingThisFrame && Npc.teleportedLastFrame)
                    {
                        Npc.teleportedLastFrame = false;
                    }

                    if (Npc.jetpackControls || Npc.disablingJetpackControls)
                    {
                        if (!this.TeleportingThisFrame && !Npc.inSpecialInteractAnimation && !Npc.enteringSpecialAnimation && !Npc.isClimbingLadder && (instanceSOR.timeSinceRoundStarted > 1f || instanceSOR.testRoom != null))
                        {
                            float magnitude2 = Npc.thisController.velocity.magnitude;
                            if (Npc.getAverageVelocityInterval <= 0f)
                            {
                                Npc.getAverageVelocityInterval = 0.04f;
                                Npc.velocityAverageCount++;
                                if (Npc.velocityAverageCount > Npc.velocityMovingAverageLength)
                                {
                                    Npc.averageVelocity += (magnitude2 - Npc.averageVelocity) / (float)(Npc.velocityMovingAverageLength + 1);
                                }
                                else
                                {
                                    Npc.averageVelocity += magnitude2;
                                    if (Npc.velocityAverageCount == Npc.velocityMovingAverageLength)
                                    {
                                        Npc.averageVelocity /= (float)Npc.velocityAverageCount;
                                    }
                                }
                            }
                            else
                            {
                                Npc.getAverageVelocityInterval -= Time.deltaTime;
                            }
                            Debug.Log(string.Format("Average velocity: {0}", Npc.averageVelocity));
                            if (TimeSinceTakingGravityDamage > 0.6f && Npc.velocityAverageCount > 4)
                            {
                                float num8 = Vector3.Angle(Npc.transform.up, Vector3.up);
                                if (Physics.CheckSphere(Npc.gameplayCamera.transform.position, 0.5f, instanceSOR.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)
                                    || (num8 > 65f && Physics.CheckSphere(Npc.lowerSpine.position, 0.5f, instanceSOR.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
                                {
                                    if (Npc.averageVelocity > 17f)
                                    {
                                        Debug.Log("Take damage a");
                                        TimeSinceTakingGravityDamage = 0f;
                                        this.InternAIOwner?.SyncDamageIntern(Mathf.Clamp(85, 20, 100), CauseOfDeath.Gravity, 0, true, Vector3.ClampMagnitude(Npc.velocityLastFrame, 50f));
                                    }
                                    else if (Npc.averageVelocity > 9f)
                                    {
                                        Debug.Log("Take damage b");
                                        this.InternAIOwner?.SyncDamageIntern(Mathf.Clamp(30, 20, 100), CauseOfDeath.Gravity, 0, true, Vector3.ClampMagnitude(Npc.velocityLastFrame, 50f));
                                        TimeSinceTakingGravityDamage = 0.35f;
                                    }
                                    else if (num8 > 60f && Npc.averageVelocity > 6f)
                                    {
                                        Debug.Log("Take damage c");
                                        this.InternAIOwner?.SyncDamageIntern(Mathf.Clamp(30, 20, 100), CauseOfDeath.Gravity, 0, true, Vector3.ClampMagnitude(Npc.velocityLastFrame, 50f));
                                        TimeSinceTakingGravityDamage = 0f;
                                    }
                                }
                            }
                            else
                            {
                                TimeSinceTakingGravityDamage += Time.deltaTime;
                            }
                            Npc.velocityLastFrame = Npc.thisController.velocity;
                            PreviousFrameDeltaTime = Time.deltaTime;
                        }
                        else
                        {
                            TeleportingThisFrame = false;
                        }
                    }
                    else
                    {
                        Npc.averageVelocity = 0f;
                        Npc.velocityAverageCount = 0;
                        TimeSinceTakingGravityDamage = 0f;
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

                    if (goDownLadder)
                    {
                        direction = -Npc.thisPlayerBody.up;
                        origin = Npc.gameplayCamera.transform.position;// Npc.transform.position;
                    }
                    if (!Physics.Raycast(origin, direction, 0.15f, instanceSOR.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
                    {
                        Npc.thisPlayerBody.transform.position += direction * (Const.BASE_MAX_SPEED * Npc.climbSpeed * Time.deltaTime);
                    }
                }
                TeleportingThisFrame = false;

                // Rotations
                this.UpdateLookAt();

                Npc.playerEye.position = Npc.gameplayCamera.transform.position;
                Npc.playerEye.rotation = Npc.gameplayCamera.transform.rotation;

                //if (Npc.isHoldingObject && Npc.currentlyHeldObjectServer == null)
                //{
                //    Npc.DropAllHeldItems(true, false);
                //}

                if (NetworkManager.Singleton != null && Npc.playersManager.connectedPlayersAmount > 0)
                {
                    this.UpdatePlayerLookInterval += Time.deltaTime;
                    PlayerControllerBPatch.UpdatePlayerAnimationsToOtherClients_ReversePatch(this.Npc, Npc.moveInputVector);
                }
            }
            else
            {
                if (!this.IsCameraDisabled)
                {
                    this.IsCameraDisabled = true;
                    Npc.gameplayCamera.enabled = false;
                    Npc.visorCamera.enabled = false;
                    Npc.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                    Npc.thisPlayerModelArms.enabled = false;
                    Npc.mapRadarDirectionIndicator.enabled = false;
                    Npc.gameObject.GetComponent<CharacterController>().enabled = false;
                    if (Npc.playerBodyAnimator.runtimeAnimatorController != Npc.playersManager.otherClientsAnimatorController)
                    {
                        Npc.playerBodyAnimator.runtimeAnimatorController = Npc.playersManager.otherClientsAnimatorController;
                    }

                    if (Npc.gameObject.GetComponent<Rigidbody>())
                    {
                        Npc.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
                    }
                }

                // Sync position and rotations
                if (!Npc.isPlayerDead && Npc.isPlayerControlled)
                {
                    if (!Npc.disableSyncInAnimation)
                    {
                        if (Npc.snapToServerPosition)
                        {
                            Npc.transform.localPosition = Vector3.Lerp(Npc.transform.localPosition, Npc.serverPlayerPosition, 16f * Time.deltaTime);
                        }
                        else
                        {
                            float num10 = 8f;
                            if (Npc.jetpackControls)
                            {
                                num10 = 15f;
                            }
                            float num11 = Mathf.Clamp(num10 * Vector3.Distance(Npc.transform.localPosition, Npc.serverPlayerPosition), 0.9f, 300f);
                            Npc.transform.localPosition = Vector3.MoveTowards(Npc.transform.localPosition, Npc.serverPlayerPosition, num11 * Time.deltaTime);
                        }
                    }
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

                    // Rotations
                    this.UpdateTurnBodyTowardsDirection();
                    this.UpdateLookAt();
                    Npc.playerEye.position = Npc.gameplayCamera.transform.position;
                    Npc.playerEye.rotation = Npc.gameplayCamera.transform.rotation;
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
                    Npc.isInGameOverAnimation = 0f;
                    Npc.hasBegunSpectating = true;
                }
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
                if ((!Npc.isClimbingLadder && !Npc.inShockingMinigame) || Npc.freeRotationInInteractAnimation)
                {
                    CameraUp = Mathf.Lerp(CameraUp, 0f, 5f * Time.deltaTime);
                    Npc.gameplayCamera.transform.localEulerAngles = new Vector3(CameraUp, Npc.gameplayCamera.transform.localEulerAngles.y, Npc.gameplayCamera.transform.localEulerAngles.z);
                }
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
            Npc.meshContainer.position = Vector3.Lerp(Npc.transform.position, Npc.transform.position - Vector3.up * 2.8f, instanceSOR.playerSinkingCurve.Evaluate(Npc.sinkingValue));
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
                    if (!InternManager.Instance.IsPlayerInternOwnerLocal(Npc))
                    {
                        Npc.waterBubblesAudio.Play();
                    }
                }
                Npc.voiceMuffledByEnemy = true;
                //if (InternManager.Instance.IsPlayerInternOwnerLocal(Npc) && Npc.sinkingValue > 0.73f)
                //{
                //    HUDManager.Instance.sinkingCoveredFace = true;
                //}
            }
            //else if (InternManager.Instance.IsPlayerInternOwnerLocal(Npc))
            //{
            //    HUDManager.Instance.sinkingCoveredFace = false;
            //}
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
                && (!Npc.isPlayerDead || instanceSOR.overrideSpectateCamera)) || (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this.Npc && !instanceSOR.overrideSpectateCamera)))
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
            if (Npc.inSpecialInteractAnimation && !Npc.inShockingMinigame)
            {
                Npc.cameraLookRig1.weight = Mathf.Lerp(Npc.cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
                Npc.cameraLookRig2.weight = Mathf.Lerp(Npc.cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
            }
            else
            {
                Npc.cameraLookRig1.weight = 0.45f;
                Npc.cameraLookRig2.weight = 1f;
            }
            if (Npc.isExhausted)
            {
                this.exhaustionEffectLerp = Mathf.Lerp(this.exhaustionEffectLerp, 1f, 10f * Time.deltaTime);
            }
            else
            {
                this.exhaustionEffectLerp = Mathf.Lerp(this.exhaustionEffectLerp, 0f, 10f * Time.deltaTime);
            }
            Npc.playerBodyAnimator.SetFloat("tiredAmount", this.exhaustionEffectLerp);

            if (Physics.Raycast(Npc.lineOfSightCube.position, Npc.lineOfSightCube.forward, out hit, 10f, Npc.playersManager.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                Npc.lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, hit.distance);
            }
            else
            {
                Npc.lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, 10f);
            }
        }

        public void LateUpdate()
        {
            GameNetworkManager instanceGNM = GameNetworkManager.Instance;

            Npc.previousElevatorPosition = Npc.playersManager.elevatorTransform.position;

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            if (Npc.usernameAlpha.alpha >= 0f && instanceGNM.localPlayerController != null)
            {
                Npc.usernameAlpha.alpha -= Time.deltaTime;
                Npc.usernameBillboard.LookAt(instanceGNM.localPlayerController.localVisorTargetPoint);
            }
            else if (Npc.usernameCanvas.gameObject.activeSelf)
            {
                Npc.usernameCanvas.gameObject.SetActive(false);
            }

            if (InternManager.Instance.IsPlayerInternOwnerLocal(Npc))
            {
                this.InternLookUpdate();
                if (Npc.isPlayerControlled && !Npc.isPlayerDead)
                {
                    if (instanceGNM != null)
                    {
                        float num;
                        if (Npc.inSpecialInteractAnimation)
                        {
                            num = 0.06f;
                        }
                        else if (PlayerControllerBPatch.NearOtherPlayers_ReversePatch(Npc, Npc, 10f))
                        {
                            num = 0.1f;
                        }
                        else
                        {
                            num = 0.24f;
                        }

                        if ((Npc.oldPlayerPosition - Npc.transform.localPosition).sqrMagnitude > num || UpdatePositionForNewlyJoinedClient)
                        {
                            UpdatePositionForNewlyJoinedClient = false;
                            if (!Npc.playersManager.newGameIsLoading)
                            {
                                InternManager.Instance.GetInternAI((int)Npc.playerClientId)?.SyncUpdatePlayerPosition(Npc.thisPlayerBody.localPosition, Npc.isInElevator, Npc.isInHangarShipRoom, Npc.isExhausted, Npc.thisController.isGrounded);
                                Npc.oldPlayerPosition = Npc.transform.localPosition;
                            }
                        }

                        GrabbableObject? currentlyHeldObject = InternManager.Instance.GetInternAI((int)Npc.playerClientId)?.HeldItem;
                        if (Npc.currentlyHeldObject != null && Npc.isHoldingObject && GrabbedObjectValidated)
                        {
                            Npc.currentlyHeldObject.transform.localPosition = Npc.currentlyHeldObject.itemProperties.positionOffset;
                            Npc.currentlyHeldObject.transform.localEulerAngles = Npc.currentlyHeldObject.itemProperties.rotationOffset;
                        }
                    }

                    float num2 = 1f;
                    if (Npc.drunkness > 0.02f)
                    {
                        num2 *= Mathf.Abs(StartOfRound.Instance.drunknessSpeedEffect.Evaluate(Npc.drunkness) - 1.25f);
                    }
                    if (Npc.isSprinting)
                    {
                        Npc.sprintMeter = Mathf.Clamp(Npc.sprintMeter - Time.deltaTime / Npc.sprintTime * Npc.carryWeight * num2, 0f, 1f);
                    }
                    else if (Npc.isMovementHindered > 0)
                    {
                        if (IsWalking)
                        {
                            Npc.sprintMeter = Mathf.Clamp(Npc.sprintMeter - Time.deltaTime / Npc.sprintTime * num2 * 0.5f, 0f, 1f);
                        }
                    }
                    else
                    {
                        if (!IsWalking)
                        {
                            Npc.sprintMeter = Mathf.Clamp(Npc.sprintMeter + Time.deltaTime / (Npc.sprintTime + 4f) * num2, 0f, 1f);
                        }
                        else
                        {
                            Npc.sprintMeter = Mathf.Clamp(Npc.sprintMeter + Time.deltaTime / (Npc.sprintTime + 9f) * num2, 0f, 1f);
                        }
                        if (Npc.isExhausted && Npc.sprintMeter > 0.2f)
                        {
                            Npc.isExhausted = false;
                        }
                    }

                    if (this.limpMultiplier > 0f)
                    {
                        this.limpMultiplier -= Time.deltaTime / 1.8f;
                    }
                    if (Npc.health < 20)
                    {
                        if (Npc.healthRegenerateTimer <= 0f)
                        {
                            Npc.healthRegenerateTimer = 1f;
                            Npc.health++;
                            if (Npc.health >= 20)
                            {
                                InternManager.Instance.GetInternAI((int)Npc.playerClientId)?.SyncMakeCriticallyInjured(false);
                            }
                        }
                        else
                        {
                            Npc.healthRegenerateTimer -= Time.deltaTime;
                        }
                    }
                }
            }
            if (!Npc.inSpecialInteractAnimation && Npc.localArmsMatchCamera)
            {
                Npc.localArmsTransform.position = Npc.cameraContainerTransform.transform.position + Npc.gameplayCamera.transform.up * -0.5f;
                Npc.playerModelArmsMetarig.rotation = Npc.localArmsRotationTarget.rotation;
            }
        }

        private void InternLookUpdate()
        {
            if (!Npc.isPlayerControlled)
            {
                return;
            }

            if (Npc.playersManager.connectedPlayersAmount < 1
                || Npc.playersManager.newGameIsLoading)
            {
                return;
            }

            int newIntEnumObjectsLookingAt = (int)this.enumObjectsLookingAt;
            Vector3 newPlayerEyeToLookAt = this.positionPlayerEyeToLookAt;
            Vector3 newPositionPlayerToLookAt = this.positionToLookAt;
            Vector3 newDirectionToUpdateTurnBodyTowardsTo = this.directionToUpdateTurnBodyTowardsTo;

            if (this.oldSentIntEnumObjectsLookingAt == newIntEnumObjectsLookingAt
                && this.oldSentPositionPlayerEyeToLookAt == newPlayerEyeToLookAt
                && this.oldSentPositionToLookAt == newPositionPlayerToLookAt
                && this.oldSentDirectionToUpdateTurnBodyTowardsTo == newDirectionToUpdateTurnBodyTowardsTo)
            {
                return;
            }

            if (this.UpdatePlayerLookInterval > 0.25f && Physics.OverlapSphere(Npc.transform.position, 35f, this.PlayerMask).Length != 0)
            {
                this.UpdatePlayerLookInterval = 0f;
                if (Npc.isClimbingLadder)
                {
                    Plugin.Logger.LogDebug($"(Client = {Npc.NetworkManager.LocalClientId}) (owner = {InternAIOwner != null}) send turn body new direction {newDirectionToUpdateTurnBodyTowardsTo}");
                }

                InternManager.Instance.GetInternAI((int)Npc.playerClientId)?.SyncUpdatePlayerRotationAndLook(newDirectionToUpdateTurnBodyTowardsTo,
                                                                                                             newIntEnumObjectsLookingAt,
                                                                                                             newPlayerEyeToLookAt,
                                                                                                             newPositionPlayerToLookAt);

                this.oldSentIntEnumObjectsLookingAt = newIntEnumObjectsLookingAt;
                this.oldSentPositionPlayerEyeToLookAt = newPlayerEyeToLookAt;
                this.oldSentPositionToLookAt = newPositionPlayerToLookAt;
                this.oldSentDirectionToUpdateTurnBodyTowardsTo = newDirectionToUpdateTurnBodyTowardsTo;
            }
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
            if (this.IsJumping)
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
            if (this.IsJumping)
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
            if (this.IsJumping)
            {
                return;
            }
            if (Npc.isSprinting)
            {
                return;
            }
            this.CrouchMeter = Mathf.Min(this.CrouchMeter + 0.3f, 1.3f);
            Npc.Crouch(!Npc.isCrouching);
        }

        public void SetTurnBodyTowardsDirectionWithPosition(Vector3 positionDirection)
        {
            directionToUpdateTurnBodyTowardsTo = positionDirection - Npc.thisController.transform.position;
            directionToUpdateTurnBodyTowardsToNormalized = directionToUpdateTurnBodyTowardsTo.normalized;
        }
        public void SetTurnBodyTowardsDirection(Vector3 direction)
        {
            directionToUpdateTurnBodyTowardsTo = direction;
            directionToUpdateTurnBodyTowardsToNormalized = directionToUpdateTurnBodyTowardsTo.normalized;
        }
        private void UpdateTurnBodyTowardsDirection()
        {
            Vector3 direction = directionToUpdateTurnBodyTowardsTo;
            if (DirectionNotZero(direction.x) || DirectionNotZero(direction.z))
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                Npc.thisPlayerBody.rotation = Quaternion.Lerp(Npc.thisPlayerBody.rotation, targetRotation, Const.BODY_TURNSPEED * Time.deltaTime);
            }
        }

        public void OrderToLookAtPlayer(Vector3 positionPlayerEyeToLookAt)
        {
            this.enumObjectsLookingAt = EnumObjectsLookingAt.Player;
            this.positionPlayerEyeToLookAt = positionPlayerEyeToLookAt;
        }
        public void OrderToLookForward()
        {
            this.enumObjectsLookingAt = EnumObjectsLookingAt.Forward;
        }
        public void OrderToLookAtPosition(Vector3 positionToLookAt)
        {
            if (!Physics.Linecast(Npc.gameplayCamera.transform.position, positionToLookAt, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                this.enumObjectsLookingAt = EnumObjectsLookingAt.Position;
                this.positionToLookAt = positionToLookAt;
            }
            else
            {
                OrderToLookForward();
            }
        }
        private void UpdateLookAt()
        {
            if (Npc.inSpecialInteractAnimation)
            {
                return;
            }

            Vector3 direction;
            switch (enumObjectsLookingAt)
            {
                case EnumObjectsLookingAt.Forward:

                    Npc.gameplayCamera.transform.rotation = Quaternion.Lerp(Npc.gameplayCamera.transform.rotation, Npc.thisPlayerBody.rotation, Const.CAMERA_TURNSPEED * Time.deltaTime);
                    break;

                case EnumObjectsLookingAt.Player:

                    direction = positionPlayerEyeToLookAt - Npc.gameplayCamera.transform.position;
                    if (DirectionNotZero(direction.x) || DirectionNotZero(direction.y) || DirectionNotZero(direction.z))
                    {
                        Quaternion cameraRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));
                        Npc.gameplayCamera.transform.rotation = Quaternion.Lerp(Npc.gameplayCamera.transform.rotation, cameraRotation, Const.CAMERA_TURNSPEED * Time.deltaTime);

                        if (Vector3.Angle(Npc.gameplayCamera.transform.forward, Npc.thisPlayerBody.transform.forward) > Const.INTERN_FOV - 5f)
                        {
                            if (this.HasToMove)
                                enumObjectsLookingAt = EnumObjectsLookingAt.Forward;
                            else
                                SetTurnBodyTowardsDirectionWithPosition(positionPlayerEyeToLookAt);
                        }
                    }
                    break;

                case EnumObjectsLookingAt.Position:

                    direction = positionToLookAt - Npc.gameplayCamera.transform.position;
                    if (DirectionNotZero(direction.x) || DirectionNotZero(direction.y) || DirectionNotZero(direction.z))
                    {
                        Quaternion cameraRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));
                        Npc.gameplayCamera.transform.rotation = Quaternion.Lerp(Npc.gameplayCamera.transform.rotation, cameraRotation, Const.CAMERA_TURNSPEED * Time.deltaTime);

                        if (Vector3.Angle(Npc.gameplayCamera.transform.forward, Npc.thisPlayerBody.transform.forward) > Const.INTERN_FOV - 20f)
                        {
                            if (this.HasToMove)
                                enumObjectsLookingAt = EnumObjectsLookingAt.Forward;
                            else
                                SetTurnBodyTowardsDirectionWithPosition(positionToLookAt);
                        }
                    }
                    break;
            }

            //    Npc.transform.localScale *= 1/0.85f;
            //Npc.localItemHolder.position = Npc.GetComponentsInChildren<Transform>().First(x => x.name == "hand.R").position;
            //Npc.localItemHolder.rotation = Npc.GetComponentsInChildren<Transform>().First(x => x.name == "hand.R").rotation;
            //    Npc.transform.localScale *= 0.85f;
            //Npc.localItemHolder.rotation = Npc.thisPlayerBody.rotation;
            //Plugin.Logger.LogDebug($"update {Npc.localItemHolder.rotation}");
            //Plugin.Logger.LogDebug($"update {Npc.localItemHolder.position}");
            //Plugin.Logger.LogDebug($"update {Npc.localItemHolder.gameObject.name}, {Npc.localItemHolder.parent?.gameObject.name}, {Npc.localItemHolder.parent?.parent?.gameObject.name}");
        }

        public void OrderToGoUpDownLadder(bool hasToGoDown)
        {
            this.goDownLadder = hasToGoDown;
        }

        private bool DirectionNotZero(float direction)
        {
            return direction < -Const.EPSILON || Const.EPSILON < direction;
        }

        public bool CanUseLadder(InteractTrigger ladder)
        {
            if (ladder.usingLadder)
            {
                return false;
            }

            if ((this.Npc.isHoldingObject && !ladder.oneHandedItemAllowed)
                || (this.Npc.twoHanded &&
                                   (!ladder.twoHandedItemAllowed || ladder.specialCharacterAnimation)))
            {
                Plugin.Logger.LogDebug("no ladder cuz holding things");
                return false;
            }

            if (this.Npc.sinkingValue > 0.73f)
            {
                return false;
            }
            if (this.Npc.jetpackControls && (ladder.specialCharacterAnimation || ladder.isLadder))
            {
                return false;
            }
            if (this.Npc.isClimbingLadder)
            {
                if (ladder.isLadder)
                {
                    if (!ladder.usingLadder)
                    {
                        return false;
                    }
                }
                else if (ladder.specialCharacterAnimation)
                {
                    return false;
                }
            }
            else if (this.Npc.inSpecialInteractAnimation)
            {
                return false;
            }

            if (ladder.isPlayingSpecialAnimation)
            {
                return false;
            }

            return true;
        }

        private void SetFaceUnderwaterFilters()
        {
            if (Npc.isPlayerDead)
            {
                return;
            }
            if (Npc.isUnderwater
                && Npc.underwaterCollider != null
                && Npc.underwaterCollider.bounds.Contains(Npc.gameplayCamera.transform.position))
            {
                Npc.statusEffectAudio.volume = Mathf.Lerp(Npc.statusEffectAudio.volume, 0f, 4f * Time.deltaTime);
                this.drowningTimer -= Time.deltaTime / 10f;
                if (this.drowningTimer < 0f)
                {
                    this.drowningTimer = 1f;
                    Plugin.Logger.LogDebug($"SyncKillIntern from drowning for LOCAL client #{Npc.NetworkManager.LocalClientId}, intern object: Intern #{Npc.playerClientId}");
                    InternManager.Instance.GetInternAI((int)Npc.playerClientId)?.SyncKillIntern(Vector3.zero, true, CauseOfDeath.Drowning, 0);
                }
            }
            else
            {
                Npc.statusEffectAudio.volume = Mathf.Lerp(Npc.statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
                this.drowningTimer = Mathf.Clamp(this.drowningTimer + Time.deltaTime, 0.1f, 1f);
            }
        }
    }
}