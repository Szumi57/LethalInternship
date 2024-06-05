using GameNetcodeStuff;
using LethalInternship.AI.AIStates;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.AI
{

    // You may be wondering, how does the Example Enemy know it is from class ExampleEnemyAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    internal class InternAI : EnemyAI
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


        public static Dictionary<GrabbableObject, float> dictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        public AIState State { get; set; } = null!;
        public List<GrabbableObject> ListInvalidObjects = null!;


        private InteractTrigger[] laddersInteractTrigger = null!;
        private EntranceTeleport[] entrancesTeleportArray = null!;
        private DoorLock[] doorLocksArray = null!;


        //private Vector3 agentLastPosition;
        //private Vector3 npcControllerLastPosition;
        private float timeSinceStuck;
        private bool hasTriedToJump = false;

        [Space(3f)]
        private RaycastHit enemyRayHit;
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

        private Vector3 previousPosition;
        private Vector3 agentLocalVelocity;

        private float updateDestinationIntervalInternAI;
        private float setDestinationToPlayerIntervalInternAI;

        private float timerCheckDoor;
        private float timerCheckLadders;

        public NpcController NpcController = null!;
        public LineRenderer LineRenderer1 = null!;
        public LineRenderer LineRenderer2 = null!;
        public LineRenderer LineRenderer3 = null!;
        public LineRenderer LineRenderer4 = null!;
        public LineRenderer LineRenderer5 = null!;

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

            // AIIntervalTime
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
                    //Plugin.Logger.LogDebug($"adding ladder {i} horiz pos {interactsTrigger[i].ladderHorizontalPosition.position}");
                    //Plugin.Logger.LogDebug($"---------------");
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
            ListInvalidObjects = new List<GrabbableObject>();

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
                    base.SyncPositionToClients();
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

            Init();

            // --- old code
            timeSinceHittingLocalPlayer = 0;
            timeSinceNewRandPos = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;
        }

        public void Init()
        {
            this.ventAnimationFinished = true;
            this.transform.position = NpcController.Npc.transform.position;
            if (agent != null)
            {
                agent.Warp(NpcController.Npc.transform.position);
                this.agent.enabled = true;
                this.agent.speed = 3.5f;
            }
            this.isEnemyDead = false;
            this.enabled = true;

            // Behaviour states
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetNames(typeof(EnumAIStates)).Length];
            int index = 0;
            foreach (var state in (EnumAIStates[])Enum.GetValues(typeof(EnumAIStates)))
            {
                enemyBehaviourStates[index++] = new EnemyBehaviourState() { name = state.ToString() };
            }
            currentBehaviourStateIndex = -1;
            State = new SearchingForPlayerState(this);
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
                this.enabled = false;
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

            //Transform t = NpcController.Npc.GetComponentsInChildren<Transform>().First(x => x.name == "hand.R");
            //Ray scanHoleRay = new Ray(t.position, t.forward);
            //Ray scanHoleRay = new Ray(NpcController.Npc.localItemHolder.position, NpcController.Npc.localItemHolder.forward);
            //float lengthScanHoleRay = 1f;
            //DrawUtil.DrawLine(LineRenderer, scanHoleRay, lengthScanHoleRay, UnityEngine.Color.red);

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

            State.DoAI();

            CheckIfStuck();
        }

        public void OrderMoveToDestination()
        {
            if (agent.isActiveAndEnabled)
            {
                agent.SetDestination(destination);
            }
            NpcController.OrderToMove();
        }

        public void StopMoving()
        {
            NpcController.OrderToStopMoving();
            TeleportAgentAndBody(NpcController.Npc.thisController.transform.position);
        }

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

            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            // Check for target player
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

        public PlayerControllerB? CheckLOSForInternHavingTargetInLOS(float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            // Check for any interns that has target still in LOS
            for (int i = StartOfRound.Instance.allPlayerScripts.Length - InternManager.AllInternAIs.Length; i < InternManager.AllEntitiesCount; i++)
            {
                PlayerControllerB intern = StartOfRound.Instance.allPlayerScripts[i];
                if (intern.playerClientId == this.NpcController.Npc.playerClientId
                    || intern.isPlayerDead
                    || !intern.isPlayerControlled)
                {
                    continue;
                }

                InternAI? internAI = InternManager.GetInternAI(i);
                if (internAI == null
                    || internAI.targetPlayer == null
                    || internAI.State.GetAIState() == EnumAIStates.JustLostPlayer)
                {
                    continue;
                }

                Vector3 posInternCamera = intern.gameplayCamera.transform.position;
                if (Vector3.Distance(posInternCamera, eye.position) < (float)range
                    && !Physics.Linecast(eye.position, posInternCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = posInternCamera - eye.position;
                    if (Vector3.Angle(eye.forward, to) < width
                        || (proximityAwareness != -1 && Vector3.Distance(eye.position, posInternCamera) < (float)proximityAwareness))
                    {
                        if (internAI.targetPlayer == targetPlayer)
                        {
                            Plugin.Logger.LogDebug($"{this.NpcController.Npc.playerClientId} Found intern {intern.playerUsername} who knows target {targetPlayer.playerUsername}");
                            return targetPlayer;
                        }
                    }
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
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length - InternManager.AllInternAIs.Length; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!PlayerIsTargetable(player))
                {
                    continue;
                }

                Vector3 position = player.gameplayCamera.transform.position;
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

        public bool IsTargetInShipBoundsExpanded()
        {
            if (targetPlayer == null)
            {
                return false;
            }

            return targetPlayer.isInElevator || InternManager.GetExpandedShipBounds().Contains(targetPlayer.transform.position);
        }

        public bool SetDestinationToPositionInternAI(Vector3 position)
        {
            moveTowardsDestination = true;
            movingTowardsTargetPlayer = false;
            destination = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f);

            return true;
        }

        private bool StuckTeleportTry1;
        private void CheckIfStuck()
        {
            if (NpcController.HasToMove
                && !NpcController.Npc.inSpecialInteractAnimation
                && !NpcController.Npc.isClimbingLadder)
            {
                // Doors
                bool isOpeningDoor = OpenDoorIfNeeded();
                // Ladders
                bool isUsingLadder = UseLadderIfNeeded();

                // Check for hole
                if (!isOpeningDoor && !isUsingLadder)
                {
                    if (Time.timeSinceLevelLoad - State.TimeSinceUsingEntrance > Const.WAIT_TIME_TO_TELEPORT)
                    {
                        if ((this.transform.position - NpcController.Npc.transform.position).sqrMagnitude > 2.5f * 2.5f)
                        {
                            NpcController.Npc.transform.position = this.transform.position;
                            Log($"============ HOLE ???? dist {(this.transform.position - NpcController.Npc.transform.position).magnitude}");
                        }
                    }
                }

                // Check for stuck
                bool legsFreeCheck1 = !RayUtil.RayCastForwardAndDraw(LineRenderer1, NpcController.Npc.thisController.transform.position + new Vector3(0, 0.4f, 0),
                                                                NpcController.Npc.thisController.transform.forward,
                                                                0.5f);
                bool legsFreeCheck2 = !RayUtil.RayCastForwardAndDraw(LineRenderer1, NpcController.Npc.thisController.transform.position + new Vector3(0, 0.6f, 0),
                                                                NpcController.Npc.thisController.transform.forward,
                                                                0.5f);
                bool legsFreeCheck = legsFreeCheck1 && legsFreeCheck2;

                bool headFreeCheck = !RayUtil.RayCastForwardAndDraw(LineRenderer4, NpcController.Npc.thisController.transform.position + new Vector3(0, 2.1f, 0),
                                                             NpcController.Npc.thisController.transform.forward,
                                                             0.5f);
                bool headFreeWhenJumpingCheck = !RayUtil.RayCastForwardAndDraw(LineRenderer2, NpcController.Npc.thisController.transform.position + new Vector3(0, 3f, 0),
                                                             NpcController.Npc.thisController.transform.forward,
                                                             0.5f);
                if (!legsFreeCheck && headFreeCheck && headFreeWhenJumpingCheck)
                {
                    if (!NpcController.IsJumping)
                    {
                        bool canMoveCheckWhileJump = !RayUtil.RayCastForwardAndDraw(LineRenderer3, NpcController.Npc.thisController.transform.position + new Vector3(0, 1.8f, 0),
                                                             NpcController.Npc.thisController.transform.forward,
                                                             0.5f);
                        if (canMoveCheckWhileJump)
                        {
                            Log($"!legsFreeCheck && headFreeCheck && headFreeWhenJumpingCheck && canMoveCheckWhileJump -> jump");
                            PlayerControllerBPatch.JumpPerformed_ReversePatch(NpcController.Npc, new UnityEngine.InputSystem.InputAction.CallbackContext());
                        }
                    }
                }
                else if (legsFreeCheck && (!headFreeCheck || !headFreeWhenJumpingCheck))
                {
                    if (!NpcController.Npc.isCrouching)
                    {
                        bool canMoveCheckWhileCrouch = !RayUtil.RayCastForwardAndDraw(LineRenderer3, NpcController.Npc.thisController.transform.position + new Vector3(0, 1f, 0),
                                                             NpcController.Npc.thisController.transform.forward,
                                                             0.5f);
                        if (canMoveCheckWhileCrouch)
                        {
                            Log($"legsFreeCheck && (!headFreeCheck || !headFreeWhenJumpingCheck) && canMoveCheckWhileCrouch -> crouch  (unsprint too)");
                            NpcController.OrderToStopSprint();
                            NpcController.OrderToToggleCrouch();
                        }
                    }
                }
                else if (legsFreeCheck && headFreeCheck)
                {
                    if (NpcController.Npc.isCrouching)
                    {
                        Log($"uncrouch");
                        NpcController.OrderToToggleCrouch();
                    }
                }


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
                    //if (NpcController.Npc.isCrouching)
                    //{
                    //    NpcController.OrderToToggleCrouch();
                    //}

                    //if (hasTriedToJump && NpcController.Npc.thisController.isGrounded)
                    //{
                    //    agent.Warp(NpcController.Npc.transform.position);
                    //    Log($"ai catch up body after jump");
                    //    hasTriedToJump = false;
                    //}
                }

                if (timeSinceStuck > Const.TIMER_STUCK_WAY_TOO_MUCH)
                {
                    timeSinceStuck = 0f;
                    Plugin.Logger.LogDebug($"- !!! Stuck since way too much - ({Const.TIMER_STUCK_WAY_TOO_MUCH}sec) -> teleport if target known");
                    // Teleport player
                    if (this.targetPlayer != null)
                    {
                        Plugin.Logger.LogDebug($"Teleport to {this.targetPlayer.transform.position}");
                        TeleportAgentAndBody(this.targetPlayer.transform.position);
                    }
                }

                if (timeSinceStuck > Const.TIMER_STUCK_TOO_MUCH)
                {
                    Plugin.Logger.LogDebug($"- Stuck since too much - ({Const.TIMER_STUCK_TOO_MUCH}sec) -> teleport");
                    // Teleport player
                    if (StuckTeleportTry1)
                    {
                        NpcController.Npc.thisPlayerBody.transform.position = this.transform.position;
                    }
                    else
                    {
                        Plugin.Logger.LogDebug($"Teleport to {NpcController.Npc.thisPlayerBody.transform.position + NpcController.Npc.thisPlayerBody.transform.forward * 1f}");
                        TeleportAgentAndBody(NpcController.Npc.thisPlayerBody.transform.position + NpcController.Npc.thisPlayerBody.transform.forward * 1f);
                    }
                    StuckTeleportTry1 = !StuckTeleportTry1;
                }

                //if (timeSinceStuck > Const.TIMER_STUCK_AFTER_TRIED_JUMP)
                //{
                //    Plugin.Logger.LogDebug("Crouch ?");
                //    NpcController.OrderToToggleCrouch();
                //}

                //if (timeSinceStuck > Const.TIMER_STUCK)
                //{
                //    if (!hasTriedToJump)
                //    {
                //        Plugin.Logger.LogDebug("Jump ?");
                //        PlayerControllerBPatch.JumpPerformed_ReversePatch(NpcController.Npc, new UnityEngine.InputSystem.InputAction.CallbackContext());
                //        hasTriedToJump = true;
                //    }
                //}
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
                if ((entityPos1 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE
                    && (entityPos2 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE)
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
                if ((door.transform.position - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_DOOR * Const.DISTANCE_NPCBODY_FROM_DOOR)
                {
                    return door;
                }
            }
            return null;
        }

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

        private bool UseLadderIfNeeded()
        {
            if (timerCheckLadders > Const.TIMER_CHECK_LADDERS)
            {
                timerCheckLadders = 0f;

                InteractTrigger? ladder = GetLadderIfWantsToUseLadder();
                if (ladder != null && NpcController.CanUseLadder(ladder))
                {
                    InteractTriggerPatch.Interact_ReversePatch(ladder, NpcController.Npc.thisPlayerBody);
                    return true;
                }
            }
            timerCheckLadders += AIIntervalTime;
            return false;
        }

        public void AssignTargetAndSetMovingTo(PlayerControllerB newTarget)
        {
            if (this.targetPlayer != newTarget)
            {
                ChangeOwnershipOfEnemy(newTarget.actualClientId);
            }
            SetMovingTowardsTargetPlayer(newTarget);
            this.destination = RoundManager.Instance.GetNavMeshPosition(this.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
        }

        public bool HandsFree()
        {
            return PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch(this.NpcController.Npc) > -1;
        }

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

                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    return null;
                }

                float sqrDistanceEyeGameObject = (gameObjectPosition - this.eye.position).sqrMagnitude;
                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_AWARNESS * Const.INTERN_OBJECT_AWARNESS)
                {
                    if (!IsGrabbableObjectGrabbable(grabbableObject))
                    {
                        continue;
                    }
                    else
                    {
                        Log($"awareness {grabbableObject.name}");
                        return grabbableObject;
                    }
                }

                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_RANGE * Const.INTERN_OBJECT_RANGE
                    && !Physics.Linecast(eye.position, gameObjectPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = gameObjectPosition - eye.position;
                    if (Vector3.Angle(eye.forward, to) < Const.INTERN_FOV)
                    {
                        if (!IsGrabbableObjectGrabbable(grabbableObject))
                        {
                            continue;
                        }
                        else
                        {
                            Log($"LOS {grabbableObject.name}");
                            return grabbableObject;
                        }
                    }
                }
            }

            return null;
        }

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

            if (ListInvalidObjects.Contains(grabbableObject))
            {
                Plugin.Logger.LogDebug($"object {grabbableObject.name} invalid to grab");
                return false;
            }

            if ((grabbableObject.transform.position - InternManager.ShipBoundClosestPoint(grabbableObject.transform.position)).sqrMagnitude
                    < Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT)
            {
                return false;
            }

            if (dictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return false;
                }
            }
            TrimDictJustDroppedItems();

            if(this.PathIsIntersectedByLineOfSight(grabbableObject.transform.position, false, false))
            {
                Plugin.Logger.LogDebug($"object {grabbableObject.name} pathfind is not reachable");
                return false;
            }

            return true;
        }

        private static void TrimDictJustDroppedItems()
        {
            if (dictJustDroppedItems != null && dictJustDroppedItems.Count > 20)
            {
                Plugin.Logger.LogDebug($"TrimDictJustDroppedItems Count{dictJustDroppedItems.Count}");
                var itemsToClean = dictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS);
                foreach (var item in itemsToClean)
                {
                    dictJustDroppedItems.Remove(item.Key);
                }
            }
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

        public void TeleportInternAndSync(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
            TeleportInternServerRpc(pos, setOutside, isUsingEntrance);
        }
        [ServerRpc]
        public void TeleportInternServerRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            TeleportInternClientRpc(pos, setOutside, isUsingEntrance);
        }
        [ClientRpc]
        public void TeleportInternClientRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
        }
        private void TeleportIntern(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            NpcController.Npc.isInsideFactory = !setOutside;
            SetEnemyOutside(setOutside);

            TeleportAgentAndBody(pos);

            NpcController.Npc.thisPlayerBody.RotateAround(((Component)NpcController.Npc.thisPlayerBody).transform.position, Vector3.up, 180f);

            if (isUsingEntrance)
            {
                State.TimeSinceUsingEntrance = Time.timeSinceLevelLoad;
                EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
                if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
                {
                    entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                    WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0], 1f);
                }
            }
        }

        private void TeleportAgentAndBody(Vector3 pos)
        {
            if ((this.transform.position - pos).sqrMagnitude < 1f * 1f)
            {
                return;
            }

            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 5f, -1);
            if (IsOwner)
            {
                agent.enabled = false;
                this.transform.position = navMeshPosition;
                agent.enabled = true;
                if (agent.isActiveAndEnabled)
                {
                    agent.Warp(navMeshPosition);
                    agent.ResetPath();
                }
            }
            else
            {
                transform.position = navMeshPosition;
            }
            serverPosition = navMeshPosition;
            NpcController.Npc.transform.position = navMeshPosition;
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
            SwitchToBehaviourClientRpc((int)EnumAIStates.GetCloseToPlayer);
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