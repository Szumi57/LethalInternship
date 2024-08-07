﻿using GameNetcodeStuff;
using LethalInternship.AI.AIStates;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.AI
{
    /// <summary>
    /// AI for the intern.
    /// </summary>
    /// <remarks>
    /// The AI is a component attached to the <c>GameObject</c> parent of the <c>PlayerControllerB</c> for the intern.<br/>
    /// For moving the AI has a agent that pathfind to the next node each game loop,
    /// the component moves by itself, detached from the body and the body (<c>PlayerControllerB</c>) moves toward it.<br/>
    /// For piloting the body, we use <see cref="NpcController"><c>NpcController</c></see> that has a reference to the body (<c>PlayerControllerB</c>).<br/>
    /// Then the AI class use its methods to pilot the body using <c>NpcController</c>.
    /// The <c>NpcController</c> is set outside in <see cref="InternManager.InitInternSpawning"><c>InternManager.InitInternSpawning</c></see>.
    /// </remarks>
    internal class InternAI : EnemyAI
    {
        /// <summary>
        /// Dictionnary of the recently dropped object on the ground.
        /// The intern will not try to grab them for a certain time (<see cref="Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS"><c>Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS</c></see>).
        /// </summary>
        public static Dictionary<GrabbableObject, float> DictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        /// <summary>
        /// Current state of the AI.
        /// </summary>
        /// <remarks>
        /// For the behaviour of the AI, we use a State pattern,
        /// with the class <see cref="AIState"><c>AIState</c></see> 
        /// that we instanciate with one of the behaviour corresponding to <see cref="EnumAIStates"><c>EnumAIStates</c></see>.
        /// </remarks>
        public AIState State { get; set; } = null!;
        public string InternId = "Not initialized";
        /// <summary>
        /// Pilot class of the body <c>PlayerControllerB</c> of the intern.
        /// </summary>
        public NpcController NpcController = null!;

        /// <summary>
        /// Currently held item by intern
        /// </summary>
        public GrabbableObject? HeldItem = null!;
        /// <summary>
        /// Used for not teleporting too much
        /// </summary>
        public float TimeSinceTeleporting { get; set; }

        private InteractTrigger[] laddersInteractTrigger = null!;
        private EntranceTeleport[] entrancesTeleportArray = null!;
        private DoorLock[] doorLocksArray = null!;

        private Coroutine grabObjectCoroutine = null!;

        private Vector3 previousWantedDestination;
        private bool isDestinationChanged;
        private float timeSinceStuck;
        private bool StuckTeleportTry1;
        private float updateDestinationIntervalInternAI;
        private float timerCheckDoor;

        public LineRendererUtil LineRendererUtil = null!;

        private void Awake()
        {
            // Behaviour states
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetNames(typeof(EnumAIStates)).Length];
            int index = 0;
            foreach (var state in (EnumAIStates[])Enum.GetValues(typeof(EnumAIStates)))
            {
                enemyBehaviourStates[index++] = new EnemyBehaviourState() { name = state.ToString() };
            }
            currentBehaviourStateIndex = -1;
        }

        /// <summary>
        /// Start unity method.
        /// </summary>
        /// <remarks>
        /// The agent is initialized here
        /// </remarks>
        public override void Start()
        {
            this.NpcController.Awake();

            // AIIntervalTime
            if (AIIntervalTime == 0f)
            {
                AIIntervalTime = 0.3f;
            }

            try
            {
                agent = gameObject.GetComponentInChildren<NavMeshAgent>();
                agent.Warp(NpcController.Npc.transform.position);
                agent.enabled = true;
                agent.speed = Const.AGENT_SPEED;
                if (!IsOwner)
                {
                    SetClientCalculatingAI(false);
                }

                skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                if (creatureAnimator == null)
                {
                    creatureAnimator = gameObject.GetComponentInChildren<Animator>();
                }
                thisNetworkObject = gameObject.GetComponentInChildren<NetworkObject>();
                path1 = new NavMeshPath();
                openDoorSpeedMultiplier = enemyType.doorSpeedMultiplier;
            }
            catch (Exception arg)
            {
                Plugin.LogError(string.Format("Error when initializing intern variables for {0} : {1}", gameObject.name, arg));
            }
            //this.lerpTarget.SetParent(RoundManager.Instance.mapPropsContainer.transform);

            Plugin.LogDebug("Intern Spawned");
        }

        /// <summary>
        /// Initialization of the field.
        /// </summary>
        /// <remarks>
        /// This method is used as an initialization and re-initialization too.
        /// </remarks>
        public void Init()
        {
            // Ladders
            laddersInteractTrigger = RefreshLaddersList();

            // Entrances
            entrancesTeleportArray = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);

            // Doors
            doorLocksArray = Object.FindObjectsOfType<DoorLock>(includeInactive: false);

            // Grabbableobject
            HoarderBugAI.RefreshGrabbableObjectsInMapList();

            // AI init
            this.ventAnimationFinished = true;
            this.transform.position = NpcController.Npc.transform.position;
            if (agent != null)
            {
                agent.Warp(NpcController.Npc.transform.position);
                agent.enabled = true;
                agent.speed = Const.AGENT_SPEED;
            }
            this.serverPosition = transform.position;
            this.isEnemyDead = false;
            this.enabled = true;

            addPlayerVelocityToDestination = 3f;

            // Position
            if (!IsOwner && agent != null)
            {
                SetClientCalculatingAI(false);
            }

            // Line renderer used for debugging stuff
            LineRendererUtil = new LineRendererUtil(6, this.transform);

            // Init state
            State = new SearchingForPlayerState(this);
        }

        /// <summary>
        /// Update unity method.
        /// </summary>
        /// <remarks>
        /// The AI does not calculate each frame but use a timer <c>updateDestinationIntervalInternAI</c>
        /// to update every some number of ms.
        /// </remarks>
        public override void Update()
        {
            // Not owner we stop calculating AI
            if (!IsOwner)
            {
                if (currentSearch.inProgress)
                {
                    StopSearch(currentSearch);
                }

                SetClientCalculatingAI(enable: false);
                timeSinceSpawn += Time.deltaTime;
                return;
            }

            // AI dead or body dead, we kill the AI or the body,
            // whichever one is not dead yet
            if (isEnemyDead)
            {
                SetClientCalculatingAI(enable: false);
                this.enabled = false;
            }
            else if (NpcController.Npc.isPlayerDead)
            {
                base.KillEnemyOnOwnerClient(false);
                NpcController.Npc.gameObject.SetActive(false);
                return;
            }

            // Set that this client calculate the AI
            if (!inSpecialAnimation)
            {
                SetClientCalculatingAI(enable: true);
            }

            // No AI calculation if in special animation
            if (inSpecialAnimation)
            {
                return;
            }

            // No AI calculation if in special animation if climbing ladder or inSpecialInteractAnimation
            if (!NpcController.Npc.isClimbingLadder
                && (NpcController.Npc.inSpecialInteractAnimation || NpcController.Npc.enteringSpecialAnimation))
            {
                return;
            }

            // Update the position of the AI to be the one from the body
            // The AI always tries to move and the body follows
            // But the AI still need to go back to the body current position each game loop
            // Otherwise the AI just do not stop and goes too far
            if (NpcController.HasToMove)
            {
                if (NpcController.Npc.isCrouching
                    || NpcController.Npc.isMovementHindered > 0)
                {
                    agent.speed = Const.AGENT_SPEED_CROUCH;
                }
                else
                {
                    agent.speed = Const.AGENT_SPEED;
                }

                if (!NpcController.Npc.isClimbingLadder
                    && !NpcController.Npc.inSpecialInteractAnimation
                    && !NpcController.Npc.enteringSpecialAnimation)
                {
                    // Npc is following ai agent position that follows destination path
                    NpcController.SetTurnBodyTowardsDirectionWithPosition(this.transform.position);
                }

                agent.nextPosition = NpcController.Npc.thisController.transform.position;
            }
            else
            {
                this.transform.position = NpcController.Npc.thisController.transform.position;
            }

            // Update interval timer for AI calculation
            if (updateDestinationIntervalInternAI >= 0f)
            {
                updateDestinationIntervalInternAI -= Time.deltaTime;
            }
            else
            {
                // Do the actual AI calculation
                DoAIInterval();
                updateDestinationIntervalInternAI = AIIntervalTime;
            }
        }

        /// <summary>
        /// Where the AI begin its calculations.
        /// </summary>
        /// <remarks>
        /// For the behaviour of the AI, we use a State pattern,
        /// with the class <see cref="AIState"><c>AIState</c></see> 
        /// that we instanciate with one of the behaviour corresponding to <see cref="EnumAIStates"><c>EnumAIStates</c></see>.
        /// </remarks>
        public override void DoAIInterval()
        {
            if (isEnemyDead || NpcController.Npc.isPlayerDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            // Do the AI calculation behaviour for the current state
            State.DoAI();

            RayUtil.RayCastAndDrawFromPointWithColor(LineRendererUtil.GetLineRenderer(), NpcController.Npc.transform.position, this.transform.position + new Vector3(0, 0.3f, 0), Color.green);
            RayUtil.RayCastAndDrawFromPointWithColor(LineRendererUtil.GetLineRenderer(), NpcController.Npc.transform.position, this.agent.destination + new Vector3(0, 0.3f, 0), Color.blue);

            // Check if the body is stuck somehow, and try to unstuck it in various ways
            CheckIfStuck();
        }

        /// <summary>
        /// Check if the intern is blocked in his path, by doors, ladders,
        /// something is front of him, a hole
        /// </summary>
        /// <remarks>
        /// Method that still need polishing and testing.<br/>
        /// - Using raycast to check if something is in front of legs, torso, head<br/>
        /// - For checking holes, we check if the AI, the brain is not going too far from the body<br/>
        /// - If the body does not move too much for some time, we try to teleport the intern to its destination
        /// </remarks>
        private void CheckIfStuck()
        {
            if (!NpcController.HasToMove)
            {
                return;
            }

            if (NpcController.Npc.jetpackControls
                || NpcController.Npc.isClimbingLadder)
            {
                return;
            }

            if (NpcController.IsJumping || NpcController.IsFallingFromJump)
            {
                return;
            }

            // Doors
            if (OpenDoorIfNeeded())
            {
                return;
            }

            // Check for stuck
            bool legsFreeCheck1 = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                 NpcController.Npc.thisController.transform.position + new Vector3(0, 0.4f, 0),
                                                                 NpcController.Npc.thisController.transform.forward,
                                                                 0.5f);
            bool legsFreeCheck2 = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                 NpcController.Npc.thisController.transform.position + new Vector3(0, 0.6f, 0),
                                                                 NpcController.Npc.thisController.transform.forward,
                                                                 0.5f);
            bool legsFreeCheck = legsFreeCheck1 && legsFreeCheck2;

            bool headFreeCheck = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                NpcController.Npc.thisController.transform.position + new Vector3(0, 2.2f, 0),
                                                                NpcController.Npc.thisController.transform.forward,
                                                                0.5f);
            bool headFreeWhenJumpingCheck = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                           NpcController.Npc.thisController.transform.position + new Vector3(0, 3f, 0),
                                                                           NpcController.Npc.thisController.transform.forward,
                                                                           0.5f);
            if (!legsFreeCheck && headFreeCheck && headFreeWhenJumpingCheck)
            {
                bool canMoveCheckWhileJump = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                            NpcController.Npc.thisController.transform.position + new Vector3(0, 1.8f, 0),
                                                                            NpcController.Npc.thisController.transform.forward,
                                                                            0.5f);
                if (canMoveCheckWhileJump)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} !legsFreeCheck && headFreeCheck && headFreeWhenJumpingCheck && canMoveCheckWhileJump -> jump");
                    PlayerControllerBPatch.JumpPerformed_ReversePatch(NpcController.Npc, new UnityEngine.InputSystem.InputAction.CallbackContext());
                }
            }
            else if (legsFreeCheck && (!headFreeCheck || !headFreeWhenJumpingCheck))
            {
                if (!NpcController.Npc.isCrouching)
                {
                    bool canMoveCheckWhileCrouch = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                                  NpcController.Npc.thisController.transform.position + new Vector3(0, 1f, 0),
                                                                                  NpcController.Npc.thisController.transform.forward,
                                                                                  0.5f);
                    if (canMoveCheckWhileCrouch)
                    {
                        Plugin.LogDebug($"{NpcController.Npc.playerUsername} legsFreeCheck && (!headFreeCheck || !headFreeWhenJumpingCheck) && canMoveCheckWhileCrouch -> crouch  (unsprint too)");
                        NpcController.OrderToStopSprint();
                        NpcController.OrderToToggleCrouch();
                    }
                }
            }
            else if (legsFreeCheck && headFreeCheck)
            {
                if (NpcController.Npc.isCrouching)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} uncrouch");
                    NpcController.OrderToToggleCrouch();
                }
            }

            // Check for hole
            if ((this.transform.position - NpcController.Npc.transform.position).sqrMagnitude > Const.DISTANCE_CHECK_FOR_HOLES * Const.DISTANCE_CHECK_FOR_HOLES)
            {
                // Ladders ?
                bool isUsingLadder = UseLadderIfNeeded();

                if (isUsingLadder)
                {
                    timeSinceStuck = 0f;
                    return;
                }

                if (!isOutside)
                {
                    if (Time.timeSinceLevelLoad - TimeSinceTeleporting > Const.WAIT_TIME_TO_TELEPORT)
                    {
                        TimeSinceTeleporting = Time.timeSinceLevelLoad;
                        NpcController.Npc.transform.position = this.transform.position;
                        Plugin.LogDebug($"{NpcController.Npc.playerUsername}============ HOLE ???? dist {(this.transform.position - NpcController.Npc.transform.position).magnitude}");
                    }
                }
                else
                {
                    this.transform.position = NpcController.Npc.thisController.transform.position;
                }
            }

            // Controller stuck in world ?
            Vector3 forward = NpcController.Npc.thisController.transform.forward;
            if (NpcController.Npc.isMovementHindered == 0
                && (forward * Vector3.Dot(forward, NpcController.Npc.thisController.velocity)).sqrMagnitude < 0.15f * 0.15f)
            {
                Plugin.LogDebug($"{NpcController.Npc.playerUsername} TimeSinceStuck {timeSinceStuck}, vel {(forward * Vector3.Dot(forward, NpcController.Npc.thisController.velocity)).sqrMagnitude}");
                timeSinceStuck += AIIntervalTime;
            }
            else if (!NpcController.IsJumping)
            {
                // Not stuck
                timeSinceStuck = 0f;
            }

            if (timeSinceStuck > Const.TIMER_STUCK_WAY_TOO_MUCH)
            {
                timeSinceStuck = 0f;
                Plugin.LogDebug($"{NpcController.Npc.playerUsername} - !!! Stuck since way too much - ({Const.TIMER_STUCK_WAY_TOO_MUCH}sec) -> teleport if target known");
                // Teleport player
                if (this.targetPlayer != null)
                {
                    Plugin.LogDebug($"Teleport to {this.targetPlayer.transform.position}");
                    TeleportAgentAndBody(this.targetPlayer.transform.position);
                }
            }

            if (timeSinceStuck > Const.TIMER_STUCK_TOO_MUCH)
            {
                Plugin.LogDebug($"{NpcController.Npc.playerUsername} - Stuck since too much - ({Const.TIMER_STUCK_TOO_MUCH}sec) -> teleport");
                // Teleport player
                if (StuckTeleportTry1)
                {
                    NpcController.Npc.thisPlayerBody.transform.position = this.transform.position;
                }
                else
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} Teleport to {NpcController.Npc.thisPlayerBody.transform.position + NpcController.Npc.thisPlayerBody.transform.forward * 1f}");
                    TeleportAgentAndBody(NpcController.Npc.thisPlayerBody.transform.position + NpcController.Npc.thisPlayerBody.transform.forward * 1f);
                }
                StuckTeleportTry1 = !StuckTeleportTry1;
            }
        }

        /// <summary>
        /// Set the destination in <c>EnemyAI</c>, not on the agent
        /// </summary>
        /// <param name="position">the destination</param>
        public void SetDestinationToPositionInternAI(Vector3 position)
        {
            moveTowardsDestination = true;
            movingTowardsTargetPlayer = false;

            if (previousWantedDestination != position)
            {
                previousWantedDestination = position;
                isDestinationChanged = true;
                destination = position;
            }
        }

        /// <summary>
        /// Try to set the destination on the agent, if destination not reachable, try the closest possible position of the destination
        /// </summary>
        public void OrderMoveToDestination()
        {
            if (!isDestinationChanged)
            {
                return;
            }

            if (agent.isActiveAndEnabled && agent.isOnNavMesh && !isEnemyDead && !NpcController.Npc.isPlayerDead)
            {
                if (!this.SetDestinationToPosition(destination, checkForPath: true))
                {
                    destination = this.ChooseClosestNodeToPosition(NpcController.Npc.transform.position, avoidLineOfSight: false).position;
                }
                agent.SetDestination(destination);
            }
            NpcController.OrderToMove();
            isDestinationChanged = false;
        }

        public void StopMoving()
        {
            if (NpcController.HasToMove)
            {
                NpcController.OrderToStopMoving();
                TeleportAgentAndBody(NpcController.Npc.thisController.transform.position);
            }
        }

        /// <summary>
        /// Is the current client running the code is the owner of the <c>InternAI</c> ?
        /// </summary>
        /// <returns></returns>
        public bool IsClientOwnerOfIntern()
        {
            return this.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        /// <summary>
        /// Check the line of sight if the intern can see the target player
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <returns>Target player <c>PlayerControllerB</c> or null</returns>
        public PlayerControllerB? CheckLOSForTarget(float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            if (targetPlayer == null)
            {
                return null;
            }

            if (!PlayerIsTargetable(targetPlayer))
            {
                return null;
            }

            // Fog reduce the visibility
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            // Check for target player
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;
            Vector3 posTargetCamera = targetPlayer.gameplayCamera.transform.position;
            if (Vector3.Distance(posTargetCamera, thisInternCamera.position) < (float)range
                && !Physics.Linecast(thisInternCamera.position, posTargetCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                // Target close enough and nothing in between to break line of sight 
                Vector3 to = posTargetCamera - thisInternCamera.position;
                if (Vector3.Angle(thisInternCamera.forward, to) < width
                    || (proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posTargetCamera) < (float)proximityAwareness))
                {
                    // Target in FOV or proximity awareness range
                    return targetPlayer;
                }
            }

            return null;
        }

        /// <summary>
        /// Check the line of sight if the intern see another intern who see the same target player.
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <returns>Target player <c>PlayerControllerB</c> or null</returns>
        public PlayerControllerB? CheckLOSForInternHavingTargetInLOS(float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;

            // Check for any interns that has target still in LOS
            for (int i = InternManager.Instance.IndexBeginOfInterns; i < InternManager.Instance.AllEntitiesCount; i++)
            {
                PlayerControllerB intern = instanceSOR.allPlayerScripts[i];
                if (intern.playerClientId == this.NpcController.Npc.playerClientId
                    || intern.isPlayerDead
                    || !intern.isPlayerControlled)
                {
                    continue;
                }

                InternAI? internAI = InternManager.Instance.GetInternAI(i);
                if (internAI == null
                    || internAI.targetPlayer == null
                    || internAI.State.GetAIState() == EnumAIStates.JustLostPlayer)
                {
                    continue;
                }

                // Check for target player
                Vector3 posInternCamera = intern.gameplayCamera.transform.position;
                if (Vector3.Distance(posInternCamera, thisInternCamera.position) < (float)range
                    && !Physics.Linecast(thisInternCamera.position, posInternCamera, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    // Target close enough and nothing in between to break line of sight 
                    Vector3 to = posInternCamera - thisInternCamera.position;
                    if (Vector3.Angle(thisInternCamera.forward, to) < width
                        || (proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posInternCamera) < (float)proximityAwareness))
                    {
                        // Target in FOV or proximity awareness range
                        if (internAI.targetPlayer == targetPlayer)
                        {
                            Plugin.LogDebug($"{this.NpcController.Npc.playerClientId} Found intern {intern.playerUsername} who knows target {targetPlayer.playerUsername}");
                            return targetPlayer;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check the line of sight if the intern can see any player and take the closest.
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <param name="bufferDistance"></param>
        /// <returns>Target player <c>PlayerControllerB</c> or null</returns>
        public PlayerControllerB? CheckLOSForClosestPlayer(float width = 45f, int range = 60, int proximityAwareness = -1, float bufferDistance = 0f)
        {
            // Fog reduce the visibility
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;
            float currentClosestDistance = 1000f;
            int indexPlayer = -1;
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = instanceSOR.allPlayerScripts[i];

                if (!player.isPlayerControlled || player.isPlayerDead)
                {
                    continue;
                }

                // Target close enough ?
                Vector3 cameraPlayerPosition = player.gameplayCamera.transform.position;
                if ((cameraPlayerPosition - this.transform.position).sqrMagnitude > range * range)
                {
                    continue;
                }

                if (!PlayerIsTargetable(player))
                {
                    continue;
                }

                // Nothing in between to break line of sight ?
                if (Physics.Linecast(NpcController.Npc.gameplayCamera.transform.position, cameraPlayerPosition, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                Vector3 vectorInternToPlayer = cameraPlayerPosition - thisInternCamera.position;
                float distanceInternToPlayer = Vector3.Distance(thisInternCamera.position, cameraPlayerPosition);
                if ((Vector3.Angle(thisInternCamera.forward, vectorInternToPlayer) < width || (proximityAwareness != -1 && distanceInternToPlayer < (float)proximityAwareness))
                    && distanceInternToPlayer < currentClosestDistance)
                {
                    // Target in FOV or proximity awareness range
                    currentClosestDistance = distanceInternToPlayer;
                    indexPlayer = i;
                }
            }

            if (targetPlayer != null
                && indexPlayer != -1
                && targetPlayer != instanceSOR.allPlayerScripts[indexPlayer]
                && bufferDistance > 0f
                && Mathf.Abs(currentClosestDistance - Vector3.Distance(base.transform.position, targetPlayer.transform.position)) < bufferDistance)
            {
                return null;
            }

            if (indexPlayer < 0)
            {
                return null;
            }

            mostOptimalDistance = currentClosestDistance;
            return instanceSOR.allPlayerScripts[indexPlayer];
        }

        /// <summary>
        /// Check if enemy in line of sight.
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <returns>Enemy <c>EnemyAI</c> or null</returns>
        public EnemyAI? CheckLOSForEnemy(float width = 45f, int range = 20, int proximityAwareness = -1)
        {
            // Fog reduce the visibility
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            StartOfRound instanceSOR = StartOfRound.Instance;
            RoundManager instanceRM = RoundManager.Instance;
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;
            int index = -1;
            foreach (EnemyAI spawnedEnemy in instanceRM.SpawnedEnemies)
            {
                index++;

                if (spawnedEnemy.isEnemyDead)
                {
                    continue;
                }

                // Enemy close enough ?
                Vector3 positionEnemy = spawnedEnemy.transform.position;
                Vector3 directionEnemyFromCamera = positionEnemy - thisInternCamera.position;
                float sqrDistanceToEnemy = directionEnemyFromCamera.sqrMagnitude;
                if (sqrDistanceToEnemy > range * range)
                {
                    continue;
                }

                // Force collision with enemy if close enough
                // todo : see why collision of intern so big, it prevents normal collision with enemies
                // todo : make the distance different for some enemies ?
                if (sqrDistanceToEnemy < Const.COLLISION_RANGE * Const.COLLISION_RANGE)
                {
                    Collider internCollider = NpcController.Npc.GetComponentInChildren<Collider>();
                    if (internCollider != null)
                    {
                        spawnedEnemy.OnCollideWithPlayer(internCollider);
                        break;
                    }
                }

                // Obstructed
                if (Physics.Linecast(thisInternCamera.position, positionEnemy, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                // Fear range
                float? fearRange = GetFearRangeForEnemies(spawnedEnemy);
                if (!fearRange.HasValue
                    || sqrDistanceToEnemy > fearRange * fearRange)
                {
                    continue;
                }
                // Enemy in distance of fear range

                // Proximity awareness, danger
                if (proximityAwareness > -1
                    && sqrDistanceToEnemy < (float)proximityAwareness * (float)proximityAwareness)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} DANGER CLOSE \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return instanceRM.SpawnedEnemies[index];
                }

                // Line of Sight, danger
                if (Vector3.Angle(thisInternCamera.forward, directionEnemyFromCamera) < width)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} DANGER LOS \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return instanceRM.SpawnedEnemies[index];
                }
            }

            return null;
        }

        /// <summary>
        /// Check for, an enemy, the minimal distance from enemy to intern before panicking.
        /// </summary>
        /// <param name="enemy">Enemy to check</param>
        /// <returns>The minimal distance from enemy to intern before panicking, null if nothing to worry about</returns>
        private float? GetFearRangeForEnemies(EnemyAI enemy)
        {
            //Plugin.LogDebug($"enemy \"{enemy.enemyType.enemyName}\" {enemy.enemyType.name}");
            switch (enemy.enemyType.enemyName)
            {
                case "Crawler":
                case "Bunker Spider":
                case "Spring":
                case "MouthDog":
                case "ForestGiant":
                case "Butler Bees":
                    return 30f;

                case "Nutcracker":
                case "Red Locust Bees":
                    return 10f;

                case "Earth Leviathan":
                case "Blob":
                case "Clay Surgeon":
                case "Flowerman":
                case "Bush Wolf":
                    return 5f;

                case "Puffer":
                    return 2f;

                case "Centipede":
                    return 0.3f;

                case "Butler":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30f;
                    }
                    else
                    {
                        return null;
                    }

                case "Hoarding bug":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30f;
                    }
                    else
                    {
                        return null;
                    }

                case "Jester":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30f;
                    }
                    else
                    {
                        return null;
                    }

                case "RadMech":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 30f;
                    }
                    else
                    {
                        return null;
                    }

                case "Baboon hawk":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 10f;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    // Not dangerous enemies (at first sight)

                    // "Docile Locust Bees"
                    // "Manticoil"
                    // "Masked"
                    // "Girl"
                    // "Tulip Snake"
                    return null;
            }
        }

        public void ReParentIntern(Transform newParent)
        {
            foreach (NetworkObject networkObject in NpcController.Npc.GetComponentsInChildren<NetworkObject>())
            {
                networkObject.AutoObjectParentSync = false;
            }

            if (NpcController.Npc.transform.parent != newParent)
            {
                Plugin.LogDebug($"npcController.Npc.transform.parent before {NpcController.Npc.transform.parent}");
                NpcController.Npc.transform.parent = newParent;
                Plugin.LogDebug($"npcController.Npc.transform.parent after {NpcController.Npc.transform.parent}");
            }

            foreach (NetworkObject networkObject in NpcController.Npc.GetComponentsInChildren<NetworkObject>())
            {
                networkObject.AutoObjectParentSync = true;
            }
        }

        /// <summary>
        /// Is the target player in the ship or outside but close to the ship ?
        /// </summary>
        /// <returns></returns>
        public bool IsTargetInShipBoundsExpanded()
        {
            if (targetPlayer == null)
            {
                return false;
            }

            return targetPlayer.isInElevator || InternManager.Instance.GetExpandedShipBounds().Contains(targetPlayer.transform.position);
        }

        /// <summary>
        /// Is the target player in the vehicle cruiser
        /// </summary>
        /// <returns></returns>
        public VehicleController? IsTargetPlayerInCruiserVehicle()
        {
            if (targetPlayer == null
                || targetPlayer.isPlayerDead)
            {
                return null;
            }

            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
            {
                return null;
            }

            if (this.targetPlayer.physicsParent != null && this.targetPlayer.physicsParent == vehicleController.transform)
            {
                return vehicleController;
            }

            return null;
        }

        /// <summary>
        /// Search for all the loaded ladders on the map.
        /// </summary>
        /// <returns>Array of <c>InteractTrigger</c> (ladders)</returns>
        private InteractTrigger[] RefreshLaddersList()
        {
            List<InteractTrigger> ladders = new List<InteractTrigger>();
            InteractTrigger[] interactsTrigger = Resources.FindObjectsOfTypeAll<InteractTrigger>();
            for (int i = 0; i < interactsTrigger.Length; i++)
            {
                if (interactsTrigger[i] == null)
                {
                    continue;
                }

                if (interactsTrigger[i].isLadder && interactsTrigger[i].ladderHorizontalPosition != null)
                {
                    ladders.Add(interactsTrigger[i]);
                }
            }
            return ladders.ToArray();
        }

        /// <summary>
        /// Check every ladder to see if the body of intern is close to either the bottom of the ladder (wants to go up) or the top of the ladder (wants to go down).
        /// Orders the controller to set field <c>hasToGoDown</c>.
        /// </summary>
        /// <returns>The ladder to use, null if nothing close</returns>
        public InteractTrigger? GetLadderIfWantsToUseLadder()
        {
            InteractTrigger ladder;
            Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            for (int i = 0; i < laddersInteractTrigger.Length; i++)
            {
                ladder = laddersInteractTrigger[i];
                Vector3 ladderBottomPos = ladder.bottomOfLadderPosition.position;
                Vector3 ladderTopPos = ladder.topOfLadderPosition.position;

                if ((ladderBottomPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} Wants to go up on ladder");
                    // Wants to go up on ladder
                    NpcController.OrderToGoUpDownLadder(hasToGoDown: false);
                    return ladder;
                }
                else if ((ladderTopPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} Wants to go down on ladder");
                    // Wants to go down on ladder
                    NpcController.OrderToGoUpDownLadder(hasToGoDown: true);
                    return ladder;
                }
            }
            return null;
        }

        /// <summary>
        /// Is the entrance (main or fire exit) is close for the two entity position in parameters ?
        /// </summary>
        /// <remarks>
        /// Use to know if the player just used the entrance and teleported away,
        /// the intern gets close to last seen position in front of the door, we check if intern is close
        /// to the door and the last seen position too.
        /// </remarks>
        /// <param name="entityPos1">Position of entity 1</param>
        /// <param name="entityPos2">Position of entity 1</param>
        /// <returns>The entrance close for both, else null</returns>
        public EntranceTeleport? IsEntranceCloseForBoth(Vector3 entityPos1, Vector3 entityPos2)
        {
            for (int i = 0; i < entrancesTeleportArray.Length; i++)
            {
                if ((entityPos1 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE
                    && (entityPos2 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE)
                {
                    return entrancesTeleportArray[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Get the position of teleport of entrance, to teleport intern to it, if he needs to go in/out of the facility to follow player.
        /// </summary>
        /// <param name="entranceToUse"></param>
        /// <returns></returns>
        public Vector3? GetTeleportPosOfEntrance(EntranceTeleport? entranceToUse)
        {
            if (entranceToUse == null)
            {
                return null;
            }

            for (int i = 0; i < entrancesTeleportArray.Length; i++)
            {
                EntranceTeleport entrance = entrancesTeleportArray[i];
                if (entrance.entranceId == entranceToUse.entranceId
                    && entrance.isEntranceToBuilding != entranceToUse.isEntranceToBuilding)
                {
                    return entrance.entrancePoint.position;
                }
            }
            return null;
        }

        /// <summary>
        /// Check all doors to know if the intern is close enough to it to open it if necessary.
        /// </summary>
        /// <returns></returns>
        public DoorLock? GetDoorIfWantsToOpen()
        {
            Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            foreach (var door in doorLocksArray.Where(x => !x.isLocked))
            {
                if ((door.transform.position - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_DOOR * Const.DISTANCE_NPCBODY_FROM_DOOR)
                {
                    return door;
                }
            }
            return null;
        }

        /// <summary>
        /// Check the doors after some interval of ms to see if intern can open one to unstuck himself.
        /// </summary>
        /// <returns>true: a door has been opened by intern. Else false</returns>
        private bool OpenDoorIfNeeded()
        {
            if (timerCheckDoor > Const.TIMER_CHECK_DOOR)
            {
                timerCheckDoor = 0f;

                DoorLock? door = GetDoorIfWantsToOpen();
                if (door != null)
                {
                    // Prevent stuck behind open door
                    Physics.IgnoreCollision(this.NpcController.Npc.playerCollider, door.GetComponent<Collider>());

                    // Open door
                    door.OpenOrCloseDoor(NpcController.Npc);
                    door.OpenDoorAsEnemyServerRpc();
                    return true;
                }
            }
            timerCheckDoor += AIIntervalTime;
            return false;
        }

        /// <summary>
        /// Check ladders if intern needs to use one to follow player.
        /// </summary>
        /// <returns>true: the intern is using or is waiting to use the ladder, else false</returns>
        private bool UseLadderIfNeeded()
        {
            if (NpcController.Npc.isClimbingLadder)
            {
                return true;
            }

            InteractTrigger? ladder = GetLadderIfWantsToUseLadder();
            if (ladder == null)
            {
                return false;
            }

            if (NpcController.CanUseLadder(ladder))
            {
                InteractTriggerPatch.Interact_ReversePatch(ladder, NpcController.Npc.thisPlayerBody);

                // Set rotation of intern to face ladder
                NpcController.Npc.transform.rotation = ladder.ladderPlayerPositionNode.transform.rotation;
                NpcController.SetTurnBodyTowardsDirection(NpcController.Npc.transform.forward);
            }
            else
            {
                // Wait to use ladder
                this.StopMoving();
            }

            return true;
        }

        /// <summary>
        /// Is the intern holding an item ?
        /// </summary>
        /// <returns>I mean come on</returns>
        public bool AreHandsFree()
        {
            return HeldItem == null;
        }

        /// <summary>
        /// Check all object array <c>HoarderBugAI.grabbableObjectsInMap</c>, 
        /// if intern is close and can see an item to grab.
        /// </summary>
        /// <returns><c>GrabbableObject</c> if intern sees an item he can grab, else null.</returns>
        public GrabbableObject? LookingForObjectToGrab()
        {
            for (int i = 0; i < HoarderBugAI.grabbableObjectsInMap.Count; i++)
            {
                GameObject gameObject = HoarderBugAI.grabbableObjectsInMap[i];
                if (gameObject == null)
                {
                    HoarderBugAI.grabbableObjectsInMap.TrimExcess();
                    continue;
                }

                // Object not outside when ai inside and vice versa
                Vector3 gameObjectPosition = gameObject.transform.position;
                if (isOutside && gameObjectPosition.y < -100f)
                {
                    continue;
                }
                else if (!isOutside && gameObjectPosition.y > -80f)
                {
                    continue;
                }

                // Object in range ?
                float sqrDistanceEyeGameObject = (gameObjectPosition - this.eye.position).sqrMagnitude;
                if (sqrDistanceEyeGameObject > Const.INTERN_OBJECT_RANGE * Const.INTERN_OBJECT_RANGE)
                {
                    continue;
                }

                if (IsGrabbableObjectBlackListed(gameObject))
                {
                    continue;
                }
                
                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    return null;
                }

                // Object close to awareness distance ?
                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_AWARNESS * Const.INTERN_OBJECT_AWARNESS)
                {
                    if (!IsGrabbableObjectGrabbable(grabbableObject))
                    {
                        continue;
                    }
                    else
                    {
                        Plugin.LogDebug($"awareness {grabbableObject.name}");
                        return grabbableObject;
                    }
                }

                // Object visible ?
                if (!Physics.Linecast(eye.position, gameObjectPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = gameObjectPosition - eye.position;
                    if (Vector3.Angle(eye.forward, to) < Const.INTERN_FOV)
                    {
                        // Object in FOV
                        if (!IsGrabbableObjectGrabbable(grabbableObject))
                        {
                            continue;
                        }
                        else
                        {
                            Plugin.LogDebug($"LOS {grabbableObject.name}");
                            return grabbableObject;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Check all conditions for deciding if an item is grabbable or not.
        /// </summary>
        /// <param name="grabbableObject">Item to check</param>
        /// <returns></returns>
        public bool IsGrabbableObjectGrabbable(GrabbableObject grabbableObject)
        {
            if (grabbableObject == null)
            {
                return false;
            }

            if (grabbableObject.isHeld
                || grabbableObject.isInShipRoom
                || !grabbableObject.grabbable
                || grabbableObject.deactivated)
            {
                return false;
            }

            // Item dropped to close the the ship
            if ((grabbableObject.transform.position - InternManager.Instance.ShipBoundClosestPoint(grabbableObject.transform.position)).sqrMagnitude
                    < Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT)
            {
                return false;
            }

            // Item just dropped, should wait a bit before grab it again
            if (DictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return false;
                }
            }

            // Trim dictionnary if too large
            TrimDictJustDroppedItems();

            // Is the item reachable with the agent pathfind ? (only owner knows and calculate) real position of ai intern)
            if (IsOwner
                && this.PathIsIntersectedByLineOfSight(grabbableObject.transform.position, false, false))
            {
                Plugin.LogDebug($"object {grabbableObject.name} pathfind is not reachable");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Trim dictionnary if too large, trim only the dropped item since a long time
        /// </summary>
        private static void TrimDictJustDroppedItems()
        {
            if (DictJustDroppedItems != null && DictJustDroppedItems.Count > 20)
            {
                Plugin.LogDebug($"TrimDictJustDroppedItems Count{DictJustDroppedItems.Count}");
                var itemsToClean = DictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS);
                foreach (var item in itemsToClean)
                {
                    DictJustDroppedItems.Remove(item.Key);
                }
            }
        }

        private bool IsGrabbableObjectBlackListed(GameObject gameObjectToEvaluate)
        {
            // Bee nest
            if (gameObjectToEvaluate.name.Contains("RedLocustHive"))
            {
                return true;
            }

            // Dead bodies
            if (gameObjectToEvaluate.name.Contains("RagdollGrabbableObject")
                && gameObjectToEvaluate.tag == "PhysicsProp"
                && gameObjectToEvaluate.GetComponentInParent<DeadBodyInfo>() != null)
            {
                return true;
            }

            return false;
        }

        #region TeleportIntern RPC

        /// <summary>
        /// Teleport intern and send to server to call client to sync
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        public void SyncTeleportIntern(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
            TeleportInternServerRpc(pos, setOutside, isUsingEntrance);
        }
        /// <summary>
        /// Server side, call clients to sync teleport intern
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        [ServerRpc]
        private void TeleportInternServerRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            TeleportInternClientRpc(pos, setOutside, isUsingEntrance);
        }
        /// <summary>
        /// Client side, teleport intern on client, only for not the owner
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        [ClientRpc]
        private void TeleportInternClientRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
        }

        /// <summary>
        /// Teleport the intern.
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        private void TeleportIntern(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            TeleportAgentAndBody(pos, setOutside);

            if (isUsingEntrance)
            {
                NpcController.Npc.thisPlayerBody.RotateAround(((Component)NpcController.Npc.thisPlayerBody).transform.position, Vector3.up, 180f);
                TimeSinceTeleporting = Time.timeSinceLevelLoad;
                EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
                if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
                {
                    entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                }
            }
        }

        /// <summary>
        /// Teleport the brain and body of intern
        /// </summary>
        /// <param name="pos"></param>
        private void TeleportAgentAndBody(Vector3 pos, bool? setOutside = null)
        {
            // Only teleport when necessary
            if ((this.transform.position - pos).sqrMagnitude < 1f * 1f)
            {
                return;
            }

            if (setOutside.HasValue)
            {
                NpcController.Npc.isInsideFactory = !setOutside.Value;
                SetEnemyOutside(setOutside.Value);
            }
            else
            {
                if (this.isOutside && pos.y < -80f)
                {
                    NpcController.Npc.isInsideFactory = true;
                    this.SetEnemyOutside(false);
                }
                else if (!this.isOutside && pos.y > -80f)
                {
                    NpcController.Npc.isInsideFactory = false;
                    this.SetEnemyOutside(true);
                }
            }

            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 2.7f);
            serverPosition = navMeshPosition;
            // Teleport body
            NpcController.Npc.transform.position = navMeshPosition;

            if (IsOwner)
            {
                // Teleport agent and AI
                if (!agent.isActiveAndEnabled || !agent.Warp(NpcController.Npc.transform.position))
                {
                    agent.enabled = false;
                    this.transform.position = NpcController.Npc.transform.position;
                    agent.enabled = true;
                }
            }
            else
            {
                this.transform.position = navMeshPosition;
            }

        }

        public void SyncTeleportInternVehicle(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportInternVehicle(pos, enteringVehicle, networkBehaviourReferenceVehicle);
            TeleportInternVehicleServerRpc(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }

        [ServerRpc]
        private void TeleportInternVehicleServerRpc(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            TeleportInternVehicleClientRpc(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }
        [ClientRpc]
        private void TeleportInternVehicleClientRpc(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            if (IsOwner)
            {
                return;
            }
            TeleportInternVehicle(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }

        private void TeleportInternVehicle(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            TeleportAgentAndBody(pos);

            NpcController.InternAIInCruiser = enteringVehicle;

            if (NpcController.InternAIInCruiser)
            {
                if (networkBehaviourReferenceVehicle.TryGet(out VehicleController vehicleController))
                {
                    // Attach intern to vehicle
                    Plugin.LogDebug($"intern #{NpcController.Npc.playerClientId} enters vehicle");
                    this.ReParentIntern(vehicleController.transform);
                }
            }
            else
            {
                Plugin.LogDebug($"intern #{NpcController.Npc.playerClientId} exits vehicle");
                this.ReParentIntern(NpcController.Npc.playersManager.playersContainer);
            }
        }

        #endregion

        #region AssignTargetAndSetMovingTo RPC

        /// <summary>
        /// Change the ownership of the intern to the new player target,
        /// and set the destination to him.
        /// </summary>
        /// <param name="newTarget">New <c>PlayerControllerB to set the owner of intern to.</c></param>
        public void SyncAssignTargetAndSetMovingTo(PlayerControllerB newTarget)
        {
            if (this.OwnerClientId != newTarget.actualClientId)
            {
                // Changes the ownership of the intern, on server and client directly
                ChangeOwnershipOfEnemy(newTarget.actualClientId);

                if (this.IsServer)
                {
                    SyncFromAssignTargetAndSetMovingToClientRpc(newTarget.playerClientId);
                }
                else
                {
                    SyncAssignTargetAndSetMovingToServerRpc(newTarget.playerClientId);
                }
            }
            else
            {
                AssignTargetAndSetMovingTo(newTarget.playerClientId);
            }
        }

        /// <summary>
        /// Server side, call clients to sync the set destination to new target player.
        /// </summary>
        /// <param name="playerid">Id of the new target player</param>
        [ServerRpc(RequireOwnership = false)]
        private void SyncAssignTargetAndSetMovingToServerRpc(ulong playerid)
        {
            SyncFromAssignTargetAndSetMovingToClientRpc(playerid);
        }

        /// <summary>
        /// Client side, set destination to the new target player
        /// </summary>
        /// <remarks>
        /// Change the state to <c>GetCloseToPlayerState</c>
        /// </remarks>
        /// <param name="playerid">Id of the new target player</param>
        [ClientRpc]
        private void SyncFromAssignTargetAndSetMovingToClientRpc(ulong playerid)
        {
            if (!IsOwner)
            {
                return;
            }

            AssignTargetAndSetMovingTo(playerid);
        }

        private void AssignTargetAndSetMovingTo(ulong playerid)
        {
            SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerid]);

            SetDestinationToPositionInternAI(this.targetPlayer.transform.position);

            if (NpcController.InternAIInCruiser)
            {
                this.State = new PlayerInCruiserState(this, Object.FindObjectOfType<VehicleController>());
            }
            else if (this.State == null || this.State.GetAIState() != EnumAIStates.GetCloseToPlayer)
            {
                this.State = new GetCloseToPlayerState(this);
            }
        }

        #endregion

        #region UpdatePlayerPosition RPC

        /// <summary>
        /// Sync the intern position between server and clients.
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="inShipRoom">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        public void SyncUpdateInternPosition(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            if (IsServer)
            {
                UpdateInternPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
            else
            {
                UpdateInternPositionServerRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
        }

        /// <summary>
        /// Server side, call clients to sync the new position of the intern
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="inShipRoom">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        [ServerRpc]
        private void UpdateInternPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            UpdateInternPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
        }

        /// <summary>
        /// Update the intern position if not owner of intern, the owner move on his side the intern.
        /// </summary>
        /// <param name="newPos">New position of the intern controller</param>
        /// <param name="inElevator">Is the intern on the ship ?</param>
        /// <param name="isInShip">Is the intern in the ship room ?</param>
        /// <param name="exhausted">Is the intern exhausted ?</param>
        /// <param name="isPlayerGrounded">Is the intern player body touching the ground ?</param>
        [ClientRpc]
        private void UpdateInternPositionClientRpc(Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
        {
            if (NpcController == null)
            {
                return;
            }

            if (IsClientOwnerOfIntern())
            {
                // Only update if not owner
                return;
            }

            PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch(NpcController.Npc,
                                                                              newPos, inElevator, isInShip, exhausted, isPlayerGrounded);
        }

        #endregion

        #region UpdatePlayerRotation and look RPC

        /// <summary>
        /// Sync the intern body rotation and rotation of head (where he looks) between server and clients.
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        public void SyncUpdateInternRotationAndLook(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (IsServer)
            {
                UpdateInternRotationAndLookClientRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
            else
            {
                UpdateInternRotationAndLookServerRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
        }

        /// <summary>
        /// Server side, call clients to update intern body rotation and rotation of head (where he looks)
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        [ServerRpc(RequireOwnership = false)]
        private void UpdateInternRotationAndLookServerRpc(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            UpdateInternRotationAndLookClientRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
        }

        /// <summary>
        /// Client side, update the intern body rotation and rotation of head (where he looks).
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        [ClientRpc]
        private void UpdateInternRotationAndLookClientRpc(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (NpcController == null)
            {
                return;
            }

            if (IsClientOwnerOfIntern())
            {
                // Only update if not owner
                return;
            }

            NpcController.SetTurnBodyTowardsDirection(direction);
            switch ((EnumObjectsLookingAt)intEnumObjectsLookingAt)
            {
                case EnumObjectsLookingAt.Forward:
                    NpcController.OrderToLookForward();
                    break;
                case EnumObjectsLookingAt.Player:
                    NpcController.OrderToLookAtPlayer(playerEyeToLookAt);
                    break;
                case EnumObjectsLookingAt.Position:
                    NpcController.OrderToLookAtPosition(positionToLookAt);
                    break;
            }
        }

        #endregion

        #region UpdatePlayer animations RPC

        /// <summary>
        /// Server side, call client to sync changes in animation of the intern
        /// </summary>
        /// <param name="animationState">Current animation state</param>
        /// <param name="animationSpeed">Current animation speed</param>
        [ServerRpc]
        public void UpdateInternAnimationServerRpc(int animationState, float animationSpeed)
        {
            UpdateInternAnimationClientRpc(animationState, animationSpeed);
        }

        /// <summary>
        /// Client, update changes in animation of the intern
        /// </summary>
        /// <param name="animationState">Current animation state</param>
        /// <param name="animationSpeed">Current animation speed</param>
        [ClientRpc]
        private void UpdateInternAnimationClientRpc(int animationState, float animationSpeed)
        {
            if (NpcController == null)
            {
                return;
            }

            if (IsClientOwnerOfIntern())
            {
                // Only update if not owner
                return;
            }

            PlayerControllerBPatch.UpdatePlayerAnimationClientRpc_ReversePatch(NpcController.Npc,
                                                                               animationState, animationSpeed);
        }

        #endregion

        #region UpdateSpecialAnimation RPC

        /// <summary>
        /// Sync the changes in special animation of the intern body, between server and clients
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        public void UpdateInternSpecialAnimationValue(bool specialAnimation, float timed, bool climbingLadder)
        {
            if (!IsClientOwnerOfIntern())
            {
                return;
            }
            UpdateInternSpecialAnimation(specialAnimation, timed, climbingLadder);

            if (IsServer)
            {
                UpdateInternSpecialAnimationClientRpc(specialAnimation, timed, climbingLadder);
            }
            else
            {
                UpdateInternSpecialAnimationServerRpc(specialAnimation, timed, climbingLadder);
            }
        }

        /// <summary>
        /// Server side, call clients to update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        [ServerRpc]
        private void UpdateInternSpecialAnimationServerRpc(bool specialAnimation, float timed, bool climbingLadder)
        {
            UpdateInternSpecialAnimationClientRpc(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Client side, update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        [ClientRpc]
        private void UpdateInternSpecialAnimationClientRpc(bool specialAnimation, float timed, bool climbingLadder)
        {
            if (IsClientOwnerOfIntern())
            {
                return;
            }

            UpdateInternSpecialAnimation(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        private void UpdateInternSpecialAnimation(bool specialAnimation, float timed, bool climbingLadder)
        {
            if (NpcController == null)
            {
                return;
            }

            PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch(NpcController.Npc, specialAnimation, timed, climbingLadder);
            NpcController.Npc.ResetZAndXRotation();
        }

        #endregion

        #region SyncBodyPosition RPC

        /// <summary>
        /// Server side, call the clients to update the dead body of the intern
        /// </summary>
        /// <param name="newBodyPosition">New dead body position</param>
        [ServerRpc(RequireOwnership = false)]
        public void SyncDeadBodyPositionServerRpc(Vector3 newBodyPosition)
        {
            SyncDeadBodyPositionClientRpc(newBodyPosition);
        }

        /// <summary>
        /// Client side, update the dead body of the intern
        /// </summary>
        /// <param name="newBodyPosition">New dead body position</param>
        [ClientRpc]
        private void SyncDeadBodyPositionClientRpc(Vector3 newBodyPosition)
        {
            PlayerControllerBPatch.SyncBodyPositionClientRpc_ReversePatch(NpcController.Npc, newBodyPosition);
        }

        #endregion

        #region Grab item RPC

        /// <summary>
        /// Server side, call clients to make the intern grab item on their side to sync everyone
        /// </summary>
        /// <param name="networkObjectReference">Item reference over the network</param>
        [ServerRpc(RequireOwnership = false)]
        public void GrabItemServerRpc(NetworkObjectReference networkObjectReference, bool itemGiven)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                Plugin.LogError($"{NpcController.Npc.playerUsername} GrabItem for InterAI {this.InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                Plugin.LogError($"{NpcController.Npc.playerUsername} GrabItem for InterAI {this.InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (!itemGiven)
            {
                if (!IsGrabbableObjectGrabbable(grabbableObject))
                {
                    Plugin.LogDebug($"{NpcController.Npc.playerUsername} grabbableObject {grabbableObject} not grabbable");
                    return;
                }
            }

            GrabItemClientRpc(networkObjectReference);
        }

        /// <summary>
        /// Client side, make the intern grab item
        /// </summary>
        /// <param name="networkObjectReference">Item reference over the network</param>
        [ClientRpc]
        private void GrabItemClientRpc(NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                Plugin.LogError($"{NpcController.Npc.playerUsername} GrabItem for InterAI {this.InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                Plugin.LogError($"{NpcController.Npc.playerUsername} GrabItem for InterAI {this.InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (this.HeldItem == grabbableObject)
            {
                Plugin.LogError($"{NpcController.Npc.playerUsername} Try to grab already held item {grabbableObject} on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabItem(grabbableObject);
        }

        /// <summary>
        /// Make the intern grab an item like an enemy would, but update the body (<c>PlayerControllerB</c>) too.
        /// </summary>
        /// <param name="grabbableObject">Item to grab</param>
        private void GrabItem(GrabbableObject grabbableObject)
        {
            Plugin.LogInfo($"{NpcController.Npc.playerUsername} Grab item {grabbableObject} on client #{NetworkManager.LocalClientId}");
            this.HeldItem = grabbableObject;

            grabbableObject.GrabItemFromEnemy(this);
            grabbableObject.parentObject = NpcController.Npc.localItemHolder;
            grabbableObject.isHeld = true;
            grabbableObject.hasHitGround = false;
            grabbableObject.isInFactory = NpcController.Npc.isInsideFactory;

            NpcController.Npc.isHoldingObject = true;
            NpcController.Npc.twoHanded = grabbableObject.itemProperties.twoHanded;
            NpcController.Npc.twoHandedAnimation = grabbableObject.itemProperties.twoHandedAnimation;
            NpcController.Npc.carryWeight += Mathf.Clamp(grabbableObject.itemProperties.weight - 1f, 0f, 10f);
            NpcController.GrabbedObjectValidated = true;
            if (grabbableObject.itemProperties.grabSFX != null)
            {
                NpcController.Npc.itemAudio.PlayOneShot(grabbableObject.itemProperties.grabSFX, 1f);
            }

            // animations
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABINVALIDATED, false);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABVALIDATED, false);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, false);
            NpcController.Npc.playerBodyAnimator.ResetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);
            this.SetSpecialGrabAnimationBool(true, grabbableObject);

            if (this.grabObjectCoroutine != null)
            {
                base.StopCoroutine(this.grabObjectCoroutine);
            }
            this.grabObjectCoroutine = base.StartCoroutine(this.GrabAnimationCoroutine());
        }

        /// <summary>
        /// Coroutine for the grab animation
        /// </summary>
        /// <returns></returns>
        private IEnumerator GrabAnimationCoroutine()
        {
            if (this.HeldItem != null)
            {
                float grabAnimationTime = this.HeldItem.itemProperties.grabAnimationTime > 0f ? this.HeldItem.itemProperties.grabAnimationTime : 0.4f;
                yield return new WaitForSeconds(grabAnimationTime - 0.2f);
                NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABVALIDATED, true);
                NpcController.Npc.isGrabbingObjectAnimation = false;
            }
            yield break;
        }

        /// <summary>
        /// Set the animation of body to something special if the item has a special grab animation.
        /// </summary>
        /// <param name="setBool">Activate or deactivate special animation</param>
        /// <param name="item">Item that has the special grab animation</param>
        private void SetSpecialGrabAnimationBool(bool setBool, GrabbableObject? item)
        {
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, setBool);
            if (item != null
                && !string.IsNullOrEmpty(item.itemProperties.grabAnim))
            {
                try
                {
                    NpcController.SetAnimationBoolForItem(item.itemProperties.grabAnim, setBool);
                    NpcController.Npc.playerBodyAnimator.SetBool(item.itemProperties.grabAnim, setBool);
                }
                catch (Exception)
                {
                    Plugin.LogError("An item tried to set an animator bool which does not exist: " + item.itemProperties.grabAnim);
                }
            }
        }

        #endregion

        #region Drop item RPC

        /// <summary>
        /// Server side, call clients to make the intern drop the held item on their side, to sync everyone
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DropItemServerRpc()
        {
            DropItemClientRpc();
        }

        /// <summary>
        /// Client side, make the intern drop the held item
        /// </summary>
        [ClientRpc]
        private void DropItemClientRpc()
        {
            DropItem();
        }

        /// <summary>
        /// Make the intern drop his item like an enemy, but update the body (<c>PlayerControllerB</c>) too.
        /// </summary>
        private void DropItem()
        {
            Plugin.LogInfo($"Try to drop item on client #{NetworkManager.LocalClientId}");
            if (this.HeldItem == null)
            {
                Plugin.LogError($"Try to drop not held item on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabbableObject grabbableObject = this.HeldItem;
            Vector3 targetFloorPosition = grabbableObject.GetItemFloorPosition();

            grabbableObject.parentObject = null;
            grabbableObject.transform.SetParent(StartOfRound.Instance.propsContainer, true);
            grabbableObject.EnablePhysics(true);
            grabbableObject.fallTime = 0f;
            grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
            grabbableObject.targetFloorPosition = grabbableObject.transform.parent.InverseTransformPoint(targetFloorPosition);
            grabbableObject.floorYRot = -1;
            grabbableObject.DiscardItemFromEnemy();
            grabbableObject.isHeld = false;
            grabbableObject.isPocketed = false;
            this.SetSpecialGrabAnimationBool(false, grabbableObject);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, true);
            NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);

            Plugin.LogDebug($"intern dropped {grabbableObject}");
            DictJustDroppedItems[grabbableObject] = Time.realtimeSinceStartup;
            this.HeldItem = null;
            NpcController.Npc.isHoldingObject = false;
            NpcController.Npc.twoHanded = false;
            NpcController.Npc.twoHandedAnimation = false;
            NpcController.Npc.carryWeight -= Mathf.Clamp(grabbableObject.itemProperties.weight - 1f, 0f, 10f);
            NpcController.GrabbedObjectValidated = false;
        }

        #endregion

        #region Damage intern from client players RPC

        /// <summary>
        /// Server side, call client to sync the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        [ServerRpc(RequireOwnership = false)]
        public void DamageInternFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClientClientRpc(damageAmount, hitDirection, playerWhoHit);
        }

        /// <summary>
        /// Client side, update and apply the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        [ClientRpc]
        private void DamageInternFromOtherClientClientRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClient(damageAmount, hitDirection, playerWhoHit);
        }

        /// <summary>
        /// Update and apply the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        private void DamageInternFromOtherClient(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            if (NpcController == null)
            {
                return;
            }

            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (NpcController.Npc.isPlayerControlled)
            {
                CentipedeAI[] array = Object.FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].clingingToPlayer == this)
                    {
                        return;
                    }
                }
                this.DamageIntern(damageAmount, CauseOfDeath.Bludgeoning, 0, false, default(Vector3));
            }

            NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
            if (NpcController.Npc.health < 6)
            {
                NpcController.Npc.DropBlood(hitDirection, true, false);
                NpcController.Npc.bodyBloodDecals[0].SetActive(true);
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].AddBloodToBody();
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].movementAudio.PlayOneShot(StartOfRound.Instance.bloodGoreSFX);
            }
        }

        #endregion

        #region Damage intern RPC

        /// <summary>
        /// Sync the damage taken by the intern between server and clients
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        public void SyncDamageIntern(int damageNumber,
                                     CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                     int deathAnimation = 0,
                                     bool fallDamage = false,
                                     Vector3 force = default)
        {
            Plugin.LogDebug($"SyncDamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (base.IsServer)
            {
                DamageInternClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
            }
            else
            {
                DamageInternServerRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
            }
        }

        /// <summary>
        /// Server side, call clients to update and apply the damage taken by the intern
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        [ServerRpc]
        private void DamageInternServerRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageInternClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        /// <summary>
        /// Client side, update and apply the damage taken by the intern
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        [ClientRpc]
        private void DamageInternClientRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        /// <summary>
        /// Apply the damage to the intern, kill him if needed, or make critically injured
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        private void DamageIntern(int damageNumber,
                                  CauseOfDeath causeOfDeath,
                                  int deathAnimation,
                                  bool fallDamage,
                                  Vector3 force)
        {
            Plugin.LogDebug(@$"DamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId},
                            damageNumber {damageNumber}, causeOfDeath {causeOfDeath}, deathAnimation {deathAnimation}, fallDamage {fallDamage}, force {force}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            // Apply damage, if not killed, set the minimum health to 5
            if (NpcController.Npc.health - damageNumber <= 0
                && !NpcController.Npc.criticallyInjured && damageNumber < 50)
            {
                NpcController.Npc.health = 5;
            }
            else
            {
                NpcController.Npc.health = Mathf.Clamp(NpcController.Npc.health - damageNumber, 0, 100);
            }
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);

            // Kill intern if necessary
            if (NpcController.Npc.health <= 0)
            {
                if (IsClientOwnerOfIntern())
                {
                    // Call the server to spawn dead bodies
                    KillInternSpawnBodyServerRpc(spawnBody: true);
                }
                // Kill on this client side only, since we are already in a rpc send to all clients
                this.KillIntern(force, spawnBody: true, causeOfDeath, deathAnimation, positionOffset: default);
            }
            else
            {
                // Critically injured
                if (NpcController.Npc.health < 10
                    && !NpcController.Npc.criticallyInjured)
                {
                    // Client side only, since we are already in an rpc send to all clients
                    MakeCriticallyInjured();
                }
                else
                {
                    // Limit sprinting when close to death
                    if (damageNumber >= 10)
                    {
                        NpcController.Npc.sprintMeter = Mathf.Clamp(NpcController.Npc.sprintMeter + (float)damageNumber / 125f, 0f, 1f);
                    }
                }
                if (fallDamage)
                {
                    NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
                }
            }

            NpcController.Npc.takingFallDamage = false;
            if (!NpcController.Npc.inSpecialInteractAnimation)
            {
                NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_DAMAGE);
            }
            NpcController.Npc.specialAnimationWeight = 1f;
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);
        }

        /// <summary>
        /// Sync the state of critically injured of beginning to heal, between server and clients
        /// </summary>
        /// <param name="enable">true: make the intern critically injured, false: make him heal</param>
        public void SyncMakeCriticallyInjured(bool enable)
        {
            if (enable)
            {
                if (IsServer)
                {
                    MakeCriticallyInjuredClientRpc();
                }
                else
                {
                    MakeCriticallyInjuredServerRpc();
                }
            }
            else
            {
                if (IsServer)
                {
                    HealClientRpc();
                }
                else
                {
                    HealServerRpc();
                }
            }
        }

        /// <summary>
        /// Server side, call clients to update the state of critically injured
        /// </summary>
        [ServerRpc]
        private void MakeCriticallyInjuredServerRpc()
        {
            MakeCriticallyInjuredClientRpc();
        }

        /// <summary>
        /// Client side update the state of critically injured
        /// </summary>
        [ClientRpc]
        private void MakeCriticallyInjuredClientRpc()
        {
            MakeCriticallyInjured();
        }

        /// <summary>
        /// Update the state of critically injured
        /// </summary>
        private void MakeCriticallyInjured()
        {
            NpcController.Npc.bleedingHeavily = true;
            NpcController.Npc.criticallyInjured = true;
            NpcController.Npc.hasBeenCriticallyInjured = true;
        }

        /// <summary>
        /// Server side, call clients to heal the intern
        /// </summary>
        [ServerRpc]
        private void HealServerRpc()
        {
            HealClientRpc();
        }

        /// <summary>
        /// Client side, heal the intern
        /// </summary>
        [ClientRpc]
        private void HealClientRpc()
        {
            Heal();
        }

        /// <summary>
        /// Heal the intern
        /// </summary>
        private void Heal()
        {
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.criticallyInjured = false;
        }

        #endregion

        #region Kill player RPC

        /// <summary>
        /// Sync the action to kill intern between server and clients
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody">Should a body be spawned ?</param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        public void SyncKillIntern(Vector3 bodyVelocity,
                                   bool spawnBody = true,
                                   CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                   int deathAnimation = 0,
                                   Vector3 positionOffset = default(Vector3))
        {
            Plugin.LogDebug($"SyncKillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (base.IsServer)
            {
                KillInternSpawnBody(spawnBody);
                KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
            }
            else
            {
                KillInternServerRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
            }
        }

        /// <summary>
        /// Server side, call clients to do the action to kill intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        [ServerRpc]
        private void KillInternServerRpc(Vector3 bodyVelocity,
                                         bool spawnBody,
                                         CauseOfDeath causeOfDeath,
                                         int deathAnimation,
                                         Vector3 positionOffset)
        {
            KillInternSpawnBody(spawnBody);
            KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
        }

        /// <summary>
        /// Server side, spawn the ragdoll of the dead body, despawn held object if no dead body to spawn
        /// (intern eaten or disappeared in some way)
        /// </summary>
        /// <param name="spawnBody">Is there a dead body to spawn following the death of the intern ?</param>
        [ServerRpc]
        private void KillInternSpawnBodyServerRpc(bool spawnBody)
        {
            KillInternSpawnBody(spawnBody);
        }

        /// <summary>
        /// Spawn the ragdoll of the dead body, despawn held object if no dead body to spawn
        /// (intern eaten or disappeared in some way)
        /// </summary>
        /// <param name="spawnBody">Is there a dead body to spawn following the death of the intern ?</param>
        private void KillInternSpawnBody(bool spawnBody)
        {
            if (!spawnBody)
            {
                for (int i = 0; i < NpcController.Npc.ItemSlots.Length; i++)
                {
                    GrabbableObject grabbableObject = NpcController.Npc.ItemSlots[i];
                    if (grabbableObject != null)
                    {
                        grabbableObject.gameObject.GetComponent<NetworkObject>().Despawn(true);
                    }
                }
            }
            else
            {
                GameObject gameObject = Object.Instantiate<GameObject>(StartOfRound.Instance.ragdollGrabbableObjectPrefab, NpcController.Npc.playersManager.propsContainer);
                gameObject.GetComponent<NetworkObject>().Spawn(false);
                gameObject.GetComponent<RagdollGrabbableObject>().bodyID.Value = (int)NpcController.Npc.playerClientId;
            }
        }

        /// <summary>
        /// Client side, do the action to kill intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        [ClientRpc]
        private void KillInternClientRpc(Vector3 bodyVelocity,
                                                 bool spawnBody,
                                                 CauseOfDeath causeOfDeath,
                                                 int deathAnimation,
                                                 Vector3 positionOffset)
        {
            KillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
        }

        /// <summary>
        /// Do the action of killing the intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        private void KillIntern(Vector3 bodyVelocity,
                                bool spawnBody,
                                CauseOfDeath causeOfDeath,
                                int deathAnimation,
                                Vector3 positionOffset)
        {
            Plugin.LogDebug(@$"KillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}
                            bodyVelocity {bodyVelocity}, spawnBody {spawnBody}, causeOfDeath {causeOfDeath}, deathAnimation {deathAnimation}, positionOffset {positionOffset}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            // Reset body
            NpcController.Npc.isPlayerDead = true;
            NpcController.Npc.isPlayerControlled = false;
            NpcController.Npc.thisPlayerModelArms.enabled = false;
            NpcController.Npc.localVisor.position = NpcController.Npc.playersManager.notSpawnedPosition.position;
            NpcController.Npc.DisablePlayerModel(NpcController.Npc.gameObject, false, false);
            NpcController.Npc.isInsideFactory = false;
            NpcController.Npc.IsInspectingItem = false;
            NpcController.Npc.inTerminalMenu = false;
            NpcController.Npc.twoHanded = false;
            NpcController.Npc.isHoldingObject = false;
            NpcController.Npc.currentlyHeldObjectServer = null;
            NpcController.Npc.carryWeight = 1f;
            NpcController.Npc.fallValue = 0f;
            NpcController.Npc.fallValueUncapped = 0f;
            NpcController.Npc.takingFallDamage = false;
            NpcController.Npc.isSinking = false;
            NpcController.Npc.isUnderwater = false;
            PatchesUtil.FieldInfoWasUnderwaterLastFrame.SetValue(NpcController.Npc, false);
            NpcController.Npc.sourcesCausingSinking = 0;
            NpcController.Npc.sinkingValue = 0f;
            NpcController.Npc.hinderedMultiplier = 1f;
            NpcController.Npc.isMovementHindered = 0;
            NpcController.Npc.inAnimationWithEnemy = null;
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.setPositionOfDeadPlayer = true;
            NpcController.Npc.snapToServerPosition = false;
            NpcController.Npc.causeOfDeath = causeOfDeath;
            if (spawnBody)
            {
                NpcController.Npc.SpawnDeadBody((int)NpcController.Npc.playerClientId, bodyVelocity, (int)causeOfDeath, NpcController.Npc, deathAnimation, null, positionOffset);
            }
            NpcController.Npc.physicsParent = null;
            NpcController.Npc.overridePhysicsParent = null;
            NpcController.Npc.lastSyncedPhysicsParent = null;
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Clear();
            this.ReParentIntern(NpcController.Npc.playersManager.playersContainer);
            if (this.HeldItem != null)
            {
                this.DropItem();
            }
            NpcController.Npc.DisableJetpackControlsLocally();
            Plugin.LogDebug($"Ran kill intern function for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
        }

        #endregion

        #region Jump RPC

        /// <summary>
        /// Sync the intern doing a jump between server and clients
        /// </summary>
        public void SyncJump()
        {
            if (IsServer)
            {
                JumpClientRpc();
            }
            else
            {
                JumpServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the intern doing a jump
        /// </summary>
        [ServerRpc]
        private void JumpServerRpc()
        {
            JumpClientRpc();
        }

        /// <summary>
        /// Client side, update the action of intern doing a jump
        /// only for not the owner
        /// </summary>
        [ClientRpc]
        private void JumpClientRpc()
        {
            if (!IsClientOwnerOfIntern())
            {
                PlayerControllerBPatch.PlayJumpAudio_ReversePatch(this.NpcController.Npc);
            }
        }

        #endregion

        #region Land from Jump RPC

        /// <summary>
        /// Sync the landing of the jump of the intern, between server and clients
        /// </summary>
        /// <param name="fallHard"></param>
        public void SyncLandFromJump(bool fallHard)
        {
            if (IsServer)
            {
                JumpLandFromClientRpc(fallHard);
            }
            else
            {
                JumpLandFromServerRpc(fallHard);
            }
        }

        /// <summary>
        /// Server side, call clients to update the action of intern land from jump
        /// </summary>
        /// <param name="fallHard"></param>
        [ServerRpc]
        private void JumpLandFromServerRpc(bool fallHard)
        {
            JumpLandFromClientRpc(fallHard);
        }

        /// <summary>
        /// Client side, update the action of intern land from jump
        /// </summary>
        /// <param name="fallHard"></param>
        [ClientRpc]
        private void JumpLandFromClientRpc(bool fallHard)
        {
            if (fallHard)
            {
                NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
                return;
            }
            NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 0.7f);
        }

        #endregion

        #region Sinking RPC

        /// <summary>
        /// Sync the state of sink of the intern between server and clients
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        public void SyncChangeSinkingState(bool startSinking, float sinkingSpeed = 0f, int audioClipIndex = 0)
        {
            if (IsServer)
            {
                ChangeSinkingStateClientRpc(startSinking, sinkingSpeed, audioClipIndex);
            }
            else
            {
                ChangeSinkingStateServerRpc(startSinking, sinkingSpeed, audioClipIndex);
            }
        }

        /// <summary>
        /// Server side, call clients to update the state of sink of the intern
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        [ServerRpc]
        private void ChangeSinkingStateServerRpc(bool startSinking, float sinkingSpeed, int audioClipIndex)
        {
            ChangeSinkingStateClientRpc(startSinking, sinkingSpeed, audioClipIndex);
        }

        /// <summary>
        /// Client side, update the state of sink of the intern
        /// </summary>
        /// <param name="startSinking"></param>
        /// <param name="sinkingSpeed"></param>
        /// <param name="audioClipIndex"></param>
        [ClientRpc]
        private void ChangeSinkingStateClientRpc(bool startSinking, float sinkingSpeed, int audioClipIndex)
        {
            if (startSinking)
            {
                NpcController.Npc.sinkingSpeedMultiplier = sinkingSpeed;
                NpcController.Npc.isSinking = true;
                NpcController.Npc.statusEffectAudio.clip = StartOfRound.Instance.statusEffectClips[audioClipIndex];
                NpcController.Npc.statusEffectAudio.Play();
            }
            else
            {
                NpcController.Npc.statusEffectAudio.Stop();
                NpcController.Npc.isSinking = false;
                NpcController.Npc.voiceMuffledByEnemy = false;
            }
        }

        #endregion

        #region Disable Jetpack RPC

        /// <summary>
        /// Sync the disabling of jetpack mode between server and clients
        /// </summary>
        public void SyncDisableJetpackMode()
        {
            if (IsServer)
            {
                DisableJetpackModeClientRpc();
            }
            else
            {
                DisableJetpackModeServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the disabling of jetpack mode between server and clients
        /// </summary>
        [ServerRpc]
        private void DisableJetpackModeServerRpc()
        {
            DisableJetpackModeClientRpc();
        }

        /// <summary>
        /// Client side, update the disabling of jetpack mode between server and clients
        /// </summary>
        [ClientRpc]
        private void DisableJetpackModeClientRpc()
        {
            NpcController.Npc.DisableJetpackControlsLocally();
        }

        #endregion

        #region Stop performing emote RPC

        /// <summary>
        /// Sync the stopping the perfoming of emote between server and clients
        /// </summary>
        public void SyncStopPerformingEmote()
        {
            if (IsServer)
            {
                StopPerformingEmoteClientRpc();
            }
            else
            {
                StopPerformingEmoteServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to update the stopping the perfoming of emote
        /// </summary>
        [ServerRpc]
        private void StopPerformingEmoteServerRpc()
        {
            StopPerformingEmoteClientRpc();
        }

        /// <summary>
        /// Update the stopping the perfoming of emote
        /// </summary>
        [ClientRpc]
        private void StopPerformingEmoteClientRpc()
        {
            NpcController.Npc.performingEmote = false;
        }

        #endregion
    }
}