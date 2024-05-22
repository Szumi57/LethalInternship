using GameNetcodeStuff;
using LethalInternship.AI.States;
using LethalInternship.Enums;
using LethalInternship.Patches;
using LethalInternship.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace LethalInternship.AI
{

    // You may be wondering, how does the Example Enemy know it is from class ExampleEnemyAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    class InternAI : EnemyAI
    {
        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649
        public Transform turnCompass = null!;
        public Transform attackArea = null!;
#pragma warning restore 0649
        float timeSinceHittingLocalPlayer;
        float timeSinceNewRandPos;
        Vector3 positionRandomness;
        Vector3 StalkPos;
        System.Random enemyRandom = null!;
        bool isDeadAnimationDone;

        public State State { get; set; } = null!;


        private InteractTrigger[] laddersInteractTrigger = null!;
        private EntranceTeleport[] entrancesTeleportArray = null!;
        private DoorLock[] doorLocksArray = null!;

        //private Vector3 agentLastPosition;
        //private Vector3 npcControllerLastPosition;
        private float timeSinceStuck;
        private bool hasTriedToJump = false;

        [Space(3f)]
        private Transform lerpTarget;
        private RaycastHit enemyRayHit;
        public Transform animationContainer;
        private float velX;
        private float velZ;

        public float walkCheckInterval;
        private Vector3 positionLastCheck;
        private float randomLookTimer;
        private bool lostPlayerInChase;
        private float lostLOSTimer;
        private bool running;
        private bool runningRandomly;
        private bool crouching;

        private float staminaTimer;
        private Vector3 focusOnPosition;
        private float verticalLookAngle;
        private float lookAtPositionTimer;
        public Transform stareAtTransform;

        private Vector3 previousPosition;
        private Vector3 agentLocalVelocity;

        private float updateDestinationIntervalInternAI;
        private float setDestinationToPlayerIntervalInternAI;

        private float timerCheckDoor;
        private float timerCheckLadders;

        public NpcController NpcController = null!;
        public LineRenderer LineRenderer = null!;

        [Conditional("DEBUG")]
        void LogIfDebugBuild(string text)
        {
            Plugin.Logger.LogInfo(text);
        }

        void Log(string text)
        {
            Plugin.Logger.LogDebug(text);
        }

        public override void Start()
        {
            this.NpcController.Awake();
            Log("Intern Spawned");

            //todo start bloodpools ?

            // Behaviour states
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetNames(typeof(EnumStates)).Length];
            int index = 0;
            foreach (var state in (EnumStates[])Enum.GetValues(typeof(EnumStates)))
            {
                enemyBehaviourStates[index++] = new EnemyBehaviourState() { name = state.ToString() };
            }

            // AIIntervalTime
            if (AIIntervalTime == 0f)
                if (AIIntervalTime == 0f)
                {
                    AIIntervalTime = 0.3f;
                }

            // Ladders
            List<InteractTrigger> ladders = new List<InteractTrigger>();
            InteractTrigger[] interactsTrigger = Resources.FindObjectsOfTypeAll<InteractTrigger>();
            for (int i = 0; i < interactsTrigger.Length; i++)
            {
                if (interactsTrigger[i] == null)
                {
                    return;
                }


                if (interactsTrigger[i].isLadder && interactsTrigger[i].ladderHorizontalPosition != null)
                {
                    ladders.Add(interactsTrigger[i]);
                    Plugin.Logger.LogDebug($"adding ladder {i} horiz pos {interactsTrigger[i].ladderHorizontalPosition.position}");
                    Plugin.Logger.LogDebug($"---------------");
                }
            }
            laddersInteractTrigger = ladders.ToArray();
            Plugin.Logger.LogDebug($"Ladders found : {laddersInteractTrigger.Length}");

            // Entrances
            entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);

            // Doors
            doorLocksArray = UnityEngine.Object.FindObjectsOfType<DoorLock>(includeInactive: false);

            // Grabbableobject
            HoarderBugAI.RefreshGrabbableObjectsInMapList();

            try
            {
                agent = gameObject.GetComponentInChildren<NavMeshAgent>();
                skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                if (creatureAnimator == null)
                {
                    creatureAnimator = gameObject.GetComponentInChildren<Animator>();
                }
                thisNetworkObject = gameObject.GetComponentInChildren<NetworkObject>();
                serverPosition = transform.position;
                //thisEnemyIndex = RoundManager.Instance.numberOfEnemiesInScene;
                //RoundManager.Instance.numberOfEnemiesInScene++;
                //isOutside = transform.position.y > -80f;
                //if (isOutside)
                //{
                //    if (allAINodes == null || allAINodes.Length == 0)
                //    {
                //        allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                //    }
                //    if (GameNetworkManager.Instance.localPlayerController != null)
                //    {
                //        EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, false);
                //    }
                //}
                //else if (allAINodes == null || allAINodes.Length == 0)
                //{
                //    allAINodes = GameObject.FindGameObjectsWithTag("AINode");
                //}
                path1 = new NavMeshPath();
                openDoorSpeedMultiplier = enemyType.doorSpeedMultiplier;
                if (IsOwner)
                {
                    // todo see that
                    //base.SyncPositionToClients();
                }
                else
                {
                    SetClientCalculatingAI(false);
                }
            }
            catch (Exception arg)
            {
                Plugin.Logger.LogError(string.Format("Error when initializing intern variables for {0} : {1}", gameObject.name, arg));
            }
            //this.lerpTarget.SetParent(RoundManager.Instance.mapPropsContainer.transform);
            enemyRayHit = default;
            addPlayerVelocityToDestination = 3f;

            this.transform.position = NpcController.Npc.transform.position;
            agent.Warp(NpcController.Npc.transform.position);
            currentBehaviourStateIndex = -1;
            State = new SearchingForPlayerState(this);

            // --- old code
            timeSinceHittingLocalPlayer = 0;
            timeSinceNewRandPos = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;
        }

        public override void Update()
        {
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

            if (isEnemyDead)
            {
                SetClientCalculatingAI(enable: false);
            }
            else if (NpcController.Npc.isPlayerDead)
            {
                base.KillEnemyOnOwnerClient(false);
                NpcController.Npc.gameObject.SetActive(false);
                return;
            }

            if (!inSpecialAnimation)
            {
                SetClientCalculatingAI(enable: true);
            }

            if (inSpecialAnimation)
            {
                return;
            }

            //    private float timerRayCast;
            //private float timerLimitRayCast = 0.05f;
            //    if (timerRayCast > timerLimitRayCast)
            //    {
            //        timerRayCast = 0f;
            //        if (NpcController.HasToMove
            //            && !NpcController.Npc.inSpecialInteractAnimation
            //            && !NpcController.Npc.isClimbingLadder)
            //        {

            //            Vector3 axis = Vector3.Cross(NpcController.Npc.playerEye.forward, Vector3.up);
            //            if (axis == Vector3.zero) axis = Vector3.right;
            //            Ray scanRay = new Ray(NpcController.Npc.playerEye.position, Quaternion.AngleAxis(-70, axis) * NpcController.Npc.playerEye.forward);
            //            float lengthScanRay = 5f;
            //            DrawUtil.DrawLine(LineRenderer, scanRay, lengthScanRay);
            //            if (!Physics.Raycast(scanRay, lengthScanRay, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            //            {
            //                NpcController.OrderToJump();
            //                Log($"======= HOLE ???");
            //                //Ray scanHoleRay = new Ray(NpcController.Npc.playerEye.position, Quaternion.AngleAxis(-90, axis) * NpcController.Npc.playerEye.forward);
            //                //float lengthScanHoleRay = 20f;
            //                //DrawUtil.DrawLine(LineRenderer, scanHoleRay, lengthScanHoleRay, UnityEngine.Color.red);
            //                //if (!Physics.Raycast(scanRay, lengthScanRay, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            //                //{
            //                //    Log($"======= HOLE !!!!!!!!!!!!!!!");
            //                //    NpcController.OrderToJump();
            //                //}
            //            }
            //        }
            //    }
            //    timerRayCast += Time.deltaTime;

            //Transform t = NpcController.Npc.GetComponentsInChildren<Transform>().First(x => x.name == "hand.R");
            //Ray scanHoleRay = new Ray(t.position, t.forward);
            Ray scanHoleRay = new Ray(NpcController.Npc.localItemHolder.position, NpcController.Npc.localItemHolder.forward);
            float lengthScanHoleRay = 1f;
            DrawUtil.DrawLine(LineRenderer, scanHoleRay, lengthScanHoleRay, UnityEngine.Color.red);

            if (NpcController.HasToMove)
            {
                // Npc is following ai agent position that follows destination path
                NpcController.SetTurnBodyTowardsDirection(this.transform.position);
                agent.nextPosition = NpcController.Npc.thisController.transform.position;
            }

            if (updateDestinationIntervalInternAI >= 0f)
            {
                updateDestinationIntervalInternAI -= Time.deltaTime;
            }
            else
            {
                DoAIInterval();
                updateDestinationIntervalInternAI = AIIntervalTime;
            }
        }

        public override void DoAIInterval()
        {
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            };

            //Log($"dist {(this.transform.position - NpcController.Npc.transform.position).magnitude}");
            if ((this.transform.position - NpcController.Npc.transform.position).magnitude > 3f)
            {
                Log($"============ HOLE ????");
            }

            CheckIfStuck();

            State.DoAI();
        }

        public void OrderMoveToDestination()
        {
            agent.SetDestination(destination);
            NpcController.OrderToMove();
        }

        public void StopMoving()
        {
            agent.ResetPath();
            NpcController.OrderToStopMoving();
            //this.transform.position = NpcController.Npc.thisController.transform.position;
            agent.Warp(NpcController.Npc.thisController.transform.position);
        }

        public PlayerControllerB? CheckLOSForTarget(float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            if (targetPlayer == null)
            {
                return null;
            }

            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            Vector3 posTargetCamera = targetPlayer.gameplayCamera.transform.position;
            if (Vector3.Distance(posTargetCamera, eye.position) < (float)range
                && !Physics.Linecast(eye.position, posTargetCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                Vector3 to = posTargetCamera - eye.position;
                if (Vector3.Angle(eye.forward, to) < width
                    || (proximityAwareness != -1 && Vector3.Distance(eye.position, posTargetCamera) < (float)proximityAwareness))
                {
                    return targetPlayer;
                }
            }

            return null;
        }

        public PlayerControllerB? CheckLOSForClosestPlayer(float width = 45f, int range = 60, int proximityAwareness = -1, float bufferDistance = 0f)
        {
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            float num = 1000f;
            float num2 = 1000f;
            int num3 = -1;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                // todo refaire !!!!
                if (i == 31) continue;
                Vector3 position = StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position;
                if ((position - this.transform.position).sqrMagnitude > range * range)
                {
                    continue;
                }

                if (!Physics.Linecast(eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = position - eye.position;
                    num = Vector3.Distance(eye.position, position);
                    if ((Vector3.Angle(eye.forward, to) < width || (proximityAwareness != -1 && num < (float)proximityAwareness)) && num < num2)
                    {
                        num2 = num;
                        num3 = i;
                    }
                }
            }

            if (targetPlayer != null && num3 != -1 && targetPlayer != StartOfRound.Instance.allPlayerScripts[num3] && bufferDistance > 0f && Mathf.Abs(num2 - Vector3.Distance(base.transform.position, targetPlayer.transform.position)) < bufferDistance)
            {
                return null;
            }

            if (num3 < 0)
            {
                return null;
            }

            mostOptimalDistance = num2;
            return StartOfRound.Instance.allPlayerScripts[num3];
        }

        public bool SetDestinationToPositionInternAI(Vector3 position)
        {
            moveTowardsDestination = true;
            movingTowardsTargetPlayer = false;
            destination = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f);

            return true;
        }

        private void CheckIfStuck()
        {
            if (NpcController.HasToMove
                && !NpcController.Npc.inSpecialInteractAnimation
                && !NpcController.Npc.isClimbingLadder)
            {
                // Doors
                CheckForDoorToOpen();
                // Ladders
                CheckForLaddersToUse();

                // Controller stuck in world ?
                //Log($"ai progression {(this.transform.position - agentLastPosition).sqrMagnitude}");
                //if ((this.transform.position - agentLastPosition).sqrMagnitude < 0.1f * 0.1f
                //    && (NpcController.Npc.transform.position - npcControllerLastPosition).sqrMagnitude < 0.45f * 0.45f)
                if (NpcController.Npc.thisController.velocity.sqrMagnitude < 0.15f * 0.15f)
                {
                    Log($"TimeSinceStuck {timeSinceStuck}");
                    timeSinceStuck += AIIntervalTime;
                }
                else
                {
                    // Not stuck
                    timeSinceStuck = 0f;
                    //agentLastPosition = this.transform.position;
                    //npcControllerLastPosition = NpcController.Npc.transform.position;

                    // UnCrouch
                    if (NpcController.Npc.isCrouching)
                    {
                        NpcController.OrderToToggleCrouch();
                    }

                    if (hasTriedToJump && NpcController.Npc.thisController.isGrounded)
                    {
                        agent.Warp(NpcController.Npc.transform.position);
                        Log($"ai catch up body after jump");
                        hasTriedToJump = false;
                    }
                }

                if (timeSinceStuck > Const.TIMER_STUCK_TOO_MUCH)
                {
                    Plugin.Logger.LogDebug($"- Stuck since too much - ({Const.TIMER_STUCK_TOO_MUCH}sec) -> teleport");
                    // Teleport player
                    NpcController.Npc.thisPlayerBody.transform.position = this.transform.position;
                }

                if (timeSinceStuck > Const.TIMER_STUCK_AFTER_TRIED_JUMP)
                {
                    Plugin.Logger.LogDebug("Crouch ?");
                    NpcController.OrderToToggleCrouch();
                }

                if (timeSinceStuck > Const.TIMER_STUCK)
                {
                    if (!hasTriedToJump)
                    {
                        Plugin.Logger.LogDebug("Jump ?");
                        PlayerControllerBPatch.JumpPerformed_ReversePatch(NpcController.Npc, new UnityEngine.InputSystem.InputAction.CallbackContext());
                        hasTriedToJump = true;
                    }
                }
            }
            else if (!NpcController.HasToMove
                && !NpcController.Npc.inSpecialInteractAnimation
                && !NpcController.Npc.isClimbingLadder)
            {
                // UnCrouch
                if (NpcController.Npc.isCrouching)
                {
                    NpcController.OrderToToggleCrouch();
                }
            }
        }

        public InteractTrigger? GetLadderIfWantsToUseLadder()
        {
            Vector3 nextAIPos = transform.position;
            Vector3 npcBodyHorizPos = new Vector3(NpcController.Npc.thisController.transform.position.x, 0f, NpcController.Npc.thisController.transform.position.z);
            for (int i = 0; i < laddersInteractTrigger.Length; i++)
            {
                Vector3 ladderBottomPos = laddersInteractTrigger[i].bottomOfLadderPosition.position;
                Vector3 ladderTopPos = laddersInteractTrigger[i].topOfLadderPosition.position;
                Vector3 ladderHorizPos = new Vector3(laddersInteractTrigger[i].ladderHorizontalPosition.position.x,
                                                   0f,
                                                   laddersInteractTrigger[i].ladderHorizontalPosition.position.z);


                if ((ladderBottomPos - nextAIPos).sqrMagnitude < Const.DISTANCE_AI_FROM_LADDER * Const.DISTANCE_AI_FROM_LADDER
                    && (ladderHorizPos - npcBodyHorizPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
                {
                    // Ai close to bottom, npc controller close to top
                    Log($"Wants to go down on ladder");
                    // Wants to go down on ladder
                    return laddersInteractTrigger[i];
                }
                else if ((ladderTopPos - nextAIPos).sqrMagnitude < Const.DISTANCE_AI_FROM_LADDER * Const.DISTANCE_AI_FROM_LADDER
                        && (ladderHorizPos - npcBodyHorizPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
                {
                    // Ai close to top, npc controller close to bottom
                    // Wants to go up on ladder
                    Log($"Wants to go up on ladder");
                    return laddersInteractTrigger[i];
                }
            }
            return null;
        }

        public EntranceTeleport? IsEntranceClose(Vector3 entityPos)
        {
            for (int i = 0; i < entrancesTeleportArray.Length; i++)
            {
                if ((entityPos - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < 3f * 3f)
                {
                    return entrancesTeleportArray[i];
                }
            }
            return null;
        }

        public EntranceTeleport? IsEntranceCloseForBoth(Vector3 entityPos1, Vector3 entityPos2)
        {
            for (int i = 0; i < entrancesTeleportArray.Length; i++)
            {
                if ((entityPos1 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < 4f * 4f
                    && (entityPos2 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < 4f * 4f)
                {
                    return entrancesTeleportArray[i];
                }
            }
            return null;
        }

        public Vector3? GetTeleportPosOfEntrance(EntranceTeleport entranceToUse)
        {
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

        public DoorLock? GetDoorIfWantsToOpen()
        {
            Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            foreach (var door in doorLocksArray.Where(x => !x.isLocked))
            {
                //Plugin.Logger.LogDebug($"dis door {(door.transform.position - npcBodyPos).magnitude}");
                if ((door.transform.position - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_DOOR * Const.DISTANCE_NPCBODY_FROM_DOOR)
                {
                    return door;
                }
            }
            return null;
        }

        private void CheckForDoorToOpen()
        {
            if (timerCheckDoor > Const.TIMER_CHECK_DOOR)
            {
                timerCheckDoor = 0f;

                DoorLock? door = GetDoorIfWantsToOpen();
                if (door != null)
                {
                    door.OpenOrCloseDoor(NpcController.Npc);
                    door.OpenDoorAsEnemyServerRpc();
                }
            }
            timerCheckDoor += AIIntervalTime;
        }

        private void CheckForLaddersToUse()
        {
            if (timerCheckLadders > Const.TIMER_CHECK_DOOR)
            {
                timerCheckLadders = 0f;

                InteractTrigger? ladder = GetLadderIfWantsToUseLadder();
                if (ladder != null)
                {
                    Plugin.Logger.LogDebug("-> wants to use ladder");
                    ladder.Interact(NpcController.Npc.thisPlayerBody);
                    return;
                }
            }
            timerCheckLadders += AIIntervalTime;
        }

        public GameObject CheckLineOfSightForObjects()
        {
            return base.CheckLineOfSight(HoarderBugAI.grabbableObjectsInMap, Const.INTERN_FOV, Const.INTERN_OBJECT_RANGE, Const.INTERN_OBJECT_AWARNESS);
        }

        private void CalculateAnimationDirection(float maxSpeed = 1f)
        {
            creatureAnimator.SetBool("IsMoving", Vector3.Distance(transform.position, previousPosition) > 0f);
            agentLocalVelocity = transform.InverseTransformDirection(Vector3.ClampMagnitude(transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
            velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
            creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, -maxSpeed, maxSpeed));
            velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
            creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, -maxSpeed, maxSpeed));
            previousPosition = transform.position;
        }

        private void LookAndRunRandomly(bool canStartRunning = false, bool onlySetRunning = false)
        {
            randomLookTimer -= AIIntervalTime;
            if (!runningRandomly && !running)
            {
                staminaTimer = Mathf.Min(6f, staminaTimer + AIIntervalTime);
            }
            else
            {
                staminaTimer = Mathf.Max(0f, staminaTimer - AIIntervalTime);
            }
            if (randomLookTimer <= 0f)
            {
                randomLookTimer = Random.Range(0.7f, 5f);
                if (!runningRandomly)
                {
                    int num;
                    if (isOutside)
                    {
                        num = 35;
                    }
                    else
                    {
                        num = 20;
                    }
                    if (onlySetRunning)
                    {
                        num /= 3;
                    }
                    if (staminaTimer >= 5f && Random.Range(0, 100) < num)
                    {
                        running = true;
                        runningRandomly = true;
                        creatureAnimator.SetBool("Sprinting", true);
                        //SetRunningServerRpc(true);
                        return;
                    }
                    if (onlySetRunning)
                    {
                        return;
                    }
                    Vector3 onUnitSphere = Random.onUnitSphere;
                    float y = 0f;
                    if (Physics.Raycast(eye.position, onUnitSphere, 5f, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers))
                    {
                        y = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(eye.position, 12f, 5);
                    }
                    onUnitSphere.y = y;
                    //this.LookAtDirectionServerRpc(onUnitSphere, Random.Range(0.25f, 2f), Random.Range(-60f, 60f));
                    return;
                }
                else
                {
                    int num2;
                    if (isOutside)
                    {
                        num2 = 80;
                    }
                    else
                    {
                        num2 = 30;
                    }
                    if (onlySetRunning)
                    {
                        num2 /= 5;
                    }
                    if (Random.Range(0, 100) > num2 || staminaTimer <= 0f)
                    {
                        running = false;
                        runningRandomly = false;
                        staminaTimer = -6f;
                        creatureAnimator.SetBool("Running", false);
                        //SetRunningServerRpc(false);
                    }
                }
            }
        }

        public void TeleportInternAndSync(Vector3 pos, bool setOutside)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside);
            TeleportInternServerRpc(pos, setOutside);
        }
        [ServerRpc]
        public void TeleportInternServerRpc(Vector3 pos, bool setOutside)
        {
            NetworkManager networkManager = NetworkManager;
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
                        Plugin.Logger.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                    }
                    return;
                }
                ServerRpcParams serverRpcParams = new ServerRpcParams();
                FastBufferWriter fastBufferWriter = __beginSendServerRpc(657232826U, serverRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(pos);
                fastBufferWriter.WriteValueSafe(setOutside, default);
                __endSendServerRpc(ref fastBufferWriter, 657232826U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            TeleportInternClientRpc(pos, setOutside);
        }
        [ClientRpc]
        public void TeleportInternClientRpc(Vector3 pos, bool setOutside)
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter fastBufferWriter = __beginSendClientRpc(2539470808U, clientRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(pos);
                fastBufferWriter.WriteValueSafe(setOutside, default);
                __endSendClientRpc(ref fastBufferWriter, 2539470808U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            if (IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside);
        }
        private void TeleportIntern(Vector3 pos, bool setOutside)
        {
            State.TimeAtLastUsingEntrance = Time.realtimeSinceStartup;
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 5f, -1);
            if (IsOwner)
            {
                agent.enabled = false;
                transform.position = navMeshPosition;
                agent.enabled = true;
                agent.Warp(navMeshPosition);
                agent.ResetPath();
            }
            else
            {
                transform.position = navMeshPosition;
            }
            NpcController.Npc.isInsideFactory = !setOutside;
            NpcController.Npc.transform.position = navMeshPosition;
            serverPosition = navMeshPosition;
            SetEnemyOutside(setOutside);
            EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
            if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
            {
                entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0], 1f);
            }
        }



        // OLD CODE
        bool FoundClosestPlayerInRange(float range, float senseRange)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if (targetPlayer == null)
            {
                // Couldn't see a player, so we check if a player is in sensing distance instead
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        bool TargetClosestPlayerInAnyCase()
        {
            mostOptimalDistance = 2000f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if (targetPlayer == null) return false;
            return true;
        }

        void StickingInFrontOfPlayer()
        {
            // We only run this method for the host because I'm paranoid about randomness not syncing I guess
            // This is fine because the game does sync the position of the enemy.
            // Also the attack is a ClientRpc so it should always sync
            if (targetPlayer == null || !IsOwner)
            {
                return;
            }

            // Go in front of player
            positionRandomness = new Vector3(enemyRandom.Next(-2, 2), 0, enemyRandom.Next(-2, 2));
            StalkPos = targetPlayer.transform.position - Vector3.Scale(new Vector3(-5, 0, -5), targetPlayer.transform.forward) + positionRandomness;
            SetDestinationToPosition(StalkPos, checkForPath: false);
        }

        IEnumerator SwingAttack()
        {
            //SwitchToBehaviourClientRpc((int)States.HeadSwingAttackInProgress);
            StalkPos = targetPlayer.transform.position;
            SetDestinationToPosition(StalkPos);
            yield return new WaitForSeconds(0.5f);
            if (isEnemyDead)
            {
                yield break;
            }
            DoAnimationClientRpc("swingAttack");
            yield return new WaitForSeconds(0.35f);
            SwingAttackHitClientRpc();
            // In case the player has already gone away, we just yield break (basically same as return, but for IEnumerator)
            //if (currentBehaviourStateIndex != (int)States.HeadSwingAttackInProgress)
            //{
            //    yield break;
            //}
            SwitchToBehaviourClientRpc((int)EnumStates.GetCloseToPlayer);
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            Plugin.Logger.LogDebug($"collision ? 1: {other.gameObject}{other.gameObject.GetInstanceID()} 2:{this.gameObject}{this.gameObject.GetInstanceID()}");
            if (other.gameObject.GetInstanceID() == this.gameObject.GetInstanceID())
            {
                Plugin.Logger.LogDebug($"collision with hitself");
                return;
            }

            Plugin.Logger.LogDebug($"collision !");
            if (timeSinceHittingLocalPlayer < 1f)
            {
                return;
            }
            PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                Log("Intern collision with Player!");
                timeSinceHittingLocalPlayer = 0f;
                playerControllerB.DamagePlayer(20);
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (isEnemyDead)
            {
                return;
            }
            enemyHP -= force;
            if (IsOwner)
            {
                if (enemyHP <= 0 && !isEnemyDead)
                {
                    // Our death sound will be played through creatureVoice when KillEnemy() is called.
                    // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
                    // so we don't need to call a death animation ourselves.

                    StopCoroutine(SwingAttack());
                    // We need to stop our search coroutine, because the game does not do that by default.
                    StopCoroutine(searchCoroutine);
                    KillEnemyOnOwnerClient();
                }
            }
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            Log($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }

        [ClientRpc]
        public void SwingAttackHitClientRpc()
        {
            Log("SwingAttackHitClientRPC");
            int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
            Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
            if (hitColliders.Length > 0)
            {
                foreach (var player in hitColliders)
                {
                    PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                    if (playerControllerB != null)
                    {
                        Log("Swing attack hit player!");
                        timeSinceHittingLocalPlayer = 0f;
                        playerControllerB.DamagePlayer(40);
                    }
                }
            }
        }
    }
}