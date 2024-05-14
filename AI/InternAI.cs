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
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

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

        public NpcController NpcController = null!;

        public GameObject himself;

        [Conditional("DEBUG")]
        void LogIfDebugBuild(string text)
        {
            Plugin.Logger.LogInfo(text);
        }

        void Log(string text)
        {
            Plugin.Logger.LogDebug(text);
        }

        private void Awake()
        {
            Log("Awake ?");
        }

        public override void Start()
        {
            Log("Intern Spawned");

            //todo start bloodpools ?

            // Behaviour states
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetNames(typeof(EnumStates)).Length];
            int index = 0;
            foreach (var state in (EnumStates[])Enum.GetValues(typeof(EnumStates)))
            {
                enemyBehaviourStates[index++] = new EnemyBehaviourState() { name = state.ToString() };
            }

            this.transform.position = NpcController.Npc.thisController.transform.position;

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
                    Plugin.Logger.LogDebug($"adding ladder {i} top pos {interactsTrigger[i].topOfLadderPosition.position}");
                    Plugin.Logger.LogDebug($"adding ladder {i} bottom pos {interactsTrigger[i].bottomOfLadderPosition.position}");
                    Plugin.Logger.LogDebug($"adding ladder {i} player pos {interactsTrigger[i].playerPositionNode.position}");
                    Plugin.Logger.LogDebug($"---------------");
                }
            }
            laddersInteractTrigger = ladders.ToArray();
            Plugin.Logger.LogDebug($"Ladders found : {laddersInteractTrigger.Length}");

            // Entrances
            entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            //foreach (EntranceTeleport e in array)
            //{
            //    Plugin.Logger.LogDebug($"entrance point {e.entrancePoint.position}, isentrance {e.isEntranceToBuilding}");
            //}
            //PropertiesAndFieldsUtils.ListPropertiesAndFieldsOfArray(array, false);

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
                thisEnemyIndex = RoundManager.Instance.numberOfEnemiesInScene;
                RoundManager.Instance.numberOfEnemiesInScene++;
                isOutside = transform.position.y > -80f;
                if (isOutside)
                {
                    if (allAINodes == null || allAINodes.Length == 0)
                    {
                        allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                    }
                    if (GameNetworkManager.Instance.localPlayerController != null)
                    {
                        EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, false);
                    }
                }
                else if (allAINodes == null || allAINodes.Length == 0)
                {
                    allAINodes = GameObject.FindGameObjectsWithTag("AINode");
                }
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

            agent.Warp(NpcController.Npc.transform.position);
            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
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

            NpcController.Update();
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

        private Vector3 agentLastPosition;
        private Vector3 npcControllerLastPosition;
        public float TimeSinceStuck;
        public bool IsStuck { get { return TimeSinceStuck > Const.TIMER_STUCK; } }
        public override void DoAIInterval()
        {
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            };

            if (NpcController.HasToMove
                && !NpcController.Npc.inSpecialInteractAnimation
                && !NpcController.Npc.isClimbingLadder)
            {
                // UnCrouch
                if (NpcController.Npc.isCrouching && State.GetState() != EnumStates.Stuck)
                {
                    NpcController.OrderToToggleCrouch();
                }

                // Controller stuck in world ?
                Log($"ai progression {(this.transform.position - agentLastPosition).sqrMagnitude}");
                if ((this.transform.position - agentLastPosition).sqrMagnitude < 0.1f * 0.1f
                    && (NpcController.Npc.transform.position - npcControllerLastPosition).sqrMagnitude < 0.45f * 0.45f)
                {
                    Log($"TimeSinceStuck {TimeSinceStuck}");
                    TimeSinceStuck += AIIntervalTime;
                }
                else
                {
                    TimeSinceStuck = 0f;
                    agentLastPosition = this.transform.position;
                    npcControllerLastPosition = NpcController.Npc.transform.position;
                }

                if (TimeSinceStuck > Const.TIMER_STUCK
                    && State.GetState() != EnumStates.Stuck)
                {
                    State = new StuckState(this);
                }


                //if (lastAiPositions == null)
                //{
                //    lastAiPositions = new Queue<Vector3>();
                //}

                //shouldEnqueue = true;
                //foreach (Vector3 aiPosition in lastAiPositions)
                //{
                //    //Plugin.Logger.LogDebug($"aiPosition {aiPosition} =? agentLastPosition {agentLastPosition} : ");
                //    if ((aiPosition - agentLastPosition).sqrMagnitude < 0.01f * 0.01f)
                //    {
                //        stuckCounterAiPositions++;
                //        Plugin.Logger.LogDebug($"                 stuck counter {stuckCounterAiPositions}");
                //        shouldEnqueue = false;
                //    }
                //}

                //if (shouldEnqueue)
                //{
                //    lastAiPositions.Enqueue(agentLastPosition);
                //}
                //if (lastAiPositions.Count > 30)
                //{
                //    lastAiPositions.Dequeue();
                //}

                //if (stuckCounterAiPositions >= 3
                //&& State.GetState() != EnumStates.Stuck)
                //{
                //    lastAiPositions.Clear();
                //    stuckCounterAiPositions = 0;
                //    State = new StuckState(this);
                //}
            }

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
                //Log($"all ladder bottom distnace {(ladderBottom - transform.position).sqrMagnitude}");
                //if ((ladderBottomPos - thisAIPos).sqrMagnitude < 10f * 10f)
                //{
                //    Log($"this ladder bottom distance {(ladderBottomPos - thisAIPos).sqrMagnitude}");
                //    if ((ladderBottomPos - thisAIPos).sqrMagnitude < 1f)
                //    {
                //        Log($"bottom of ladder reached");
                //    }

                //    Log($"this ladder top distance {(ladderTopPos - thisAIPos).sqrMagnitude}");
                //    if ((ladderTopPos - thisAIPos).sqrMagnitude < 1f)
                //    {
                //        Log($"top of ladder reached");
                //    }
                //}

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
                if ((entityPos1 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < 3f * 3f
                    && (entityPos2 - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude < 3f * 3f)
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
                        SetRunningServerRpc(true);
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
                        SetRunningServerRpc(false);
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

        // Token: 0x060003AC RID: 940 RVA: 0x00021AE4 File Offset: 0x0001FCE4
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

        // Token: 0x060003AD RID: 941 RVA: 0x00021C24 File Offset: 0x0001FE24
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
                NpcController.Npc.transform.position = navMeshPosition;
            }
            else
            {
                transform.position = navMeshPosition;
            }
            serverPosition = navMeshPosition;
            SetEnemyOutside(setOutside);
            EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
            if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
            {
                entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0], 1f);
            }
        }

        [ServerRpc]
        public void SetRunningServerRpc(bool running)
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
                FastBufferWriter fastBufferWriter = __beginSendServerRpc(3309468324U, serverRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(running, default);
                __endSendServerRpc(ref fastBufferWriter, 3309468324U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            SetRunningClientRpc(running);
        }

        // Token: 0x060003C0 RID: 960 RVA: 0x00023674 File Offset: 0x00021874
        [ClientRpc]
        public void SetRunningClientRpc(bool setRunning)
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter fastBufferWriter = __beginSendClientRpc(3512011720U, clientRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(setRunning, default);
                __endSendClientRpc(ref fastBufferWriter, 3512011720U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            running = setRunning;
            creatureAnimator.SetBool("Running", setRunning);
        }

        [ServerRpc]
        public void LookAtDirectionServerRpc(Vector3 dir, float time, float vertLookAngle)
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
                FastBufferWriter fastBufferWriter = __beginSendServerRpc(2502006210U, serverRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(dir);
                fastBufferWriter.WriteValueSafe(time, default);
                fastBufferWriter.WriteValueSafe(vertLookAngle, default);
                __endSendServerRpc(ref fastBufferWriter, 2502006210U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            LookAtDirectionClientRpc(dir, time, vertLookAngle);
        }

        [ClientRpc]
        public void LookAtDirectionClientRpc(Vector3 dir, float time, float vertLookAngle)
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter fastBufferWriter = __beginSendClientRpc(3625708449U, clientRpcParams, RpcDelivery.Reliable);
                fastBufferWriter.WriteValueSafe(dir);
                fastBufferWriter.WriteValueSafe(time, default);
                fastBufferWriter.WriteValueSafe(vertLookAngle, default);
                __endSendClientRpc(ref fastBufferWriter, 3625708449U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            LookAtDirection(dir, time, vertLookAngle);
        }

        public void LookAtDirection(Vector3 direction, float lookAtTime = 1f, float vertLookAngle = 0f)
        {
            verticalLookAngle = vertLookAngle;
            direction = Vector3.Normalize(direction * 100f);
            focusOnPosition = transform.position + direction * 1000f;
            lookAtPositionTimer = lookAtTime;
        }

        [ServerRpc]
        public void LookAtPlayerServerRpc(int playerId)
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
                FastBufferWriter writer = __beginSendServerRpc(1141953697U, serverRpcParams, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(writer, playerId);
                __endSendServerRpc(ref writer, 1141953697U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            LookAtPlayerClientRpc(playerId);
        }

        // Token: 0x060003B5 RID: 949 RVA: 0x00022A54 File Offset: 0x00020C54
        [ClientRpc]
        public void LookAtPlayerClientRpc(int playerId)
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter writer = __beginSendClientRpc(2397761797U, clientRpcParams, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(writer, playerId);
                __endSendClientRpc(ref writer, 2397761797U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            stareAtTransform = StartOfRound.Instance.allPlayerScripts[playerId].gameplayCamera.transform;
        }


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



        [ServerRpc]
        public void StopLookingAtTransformServerRpc()
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
                FastBufferWriter fastBufferWriter = __beginSendServerRpc(1407409549U, serverRpcParams, RpcDelivery.Reliable);
                __endSendServerRpc(ref fastBufferWriter, 1407409549U, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
            {
                return;
            }
            StopLookingAtTransformClientRpc();
        }

        // Token: 0x060003B7 RID: 951 RVA: 0x00022C5C File Offset: 0x00020E5C
        [ClientRpc]
        public void StopLookingAtTransformClientRpc()
        {
            NetworkManager networkManager = NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams();
                FastBufferWriter fastBufferWriter = __beginSendClientRpc(1561581057U, clientRpcParams, RpcDelivery.Reliable);
                __endSendClientRpc(ref fastBufferWriter, 1561581057U, clientRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage != __RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            {
                return;
            }
            stareAtTransform = null;
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