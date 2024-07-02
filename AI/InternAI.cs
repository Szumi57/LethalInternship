using GameNetcodeStuff;
using LethalInternship.AI.AIStates;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Utils;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
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


        public static Dictionary<GrabbableObject, float> DictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        public AIState State { get; set; } = null!;
        public string InternId = "Not initialized";
        public NpcController NpcController = null!;

        public List<GrabbableObject> ListInvalidObjects = null!;
        public GrabbableObject? HeldItem = null!;
        public float TimeSinceUsingEntrance { get; set; }

        private InteractTrigger[] laddersInteractTrigger = null!;
        private EntranceTeleport[] entrancesTeleportArray = null!;
        private DoorLock[] doorLocksArray = null!;
        private Coroutine grabObjectCoroutine = null!;

        //private Vector3 agentLastPosition;
        //private Vector3 npcControllerLastPosition;
        private float timeSinceStuck;
        private bool hasTriedToJump = false;
        private bool StuckTeleportTry1;

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

        private LineRendererUtil LineRendererUtil = null!;

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
            // Behaviour states
            enemyBehaviourStates = new EnemyBehaviourState[Enum.GetNames(typeof(EnumAIStates)).Length];
            int index = 0;
            foreach (var state in (EnumAIStates[])Enum.GetValues(typeof(EnumAIStates)))
            {
                enemyBehaviourStates[index++] = new EnemyBehaviourState() { name = state.ToString() };
            }
            currentBehaviourStateIndex = -1;
        }

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
                agent.speed = 3.5f;
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
                Plugin.Logger.LogError(string.Format("Error when initializing intern variables for {0} : {1}", gameObject.name, arg));
            }
            //this.lerpTarget.SetParent(RoundManager.Instance.mapPropsContainer.transform);

            Log("Intern Spawned");

            // --- old code
            timeSinceHittingLocalPlayer = 0;
            timeSinceNewRandPos = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;
        }

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
            ListInvalidObjects = new List<GrabbableObject>();

            // AI init
            this.ventAnimationFinished = true;
            this.transform.position = NpcController.Npc.transform.position;
            if (agent != null)
            {
                agent.Warp(NpcController.Npc.transform.position);
                agent.enabled = true;
                agent.speed = 3.5f;
            }
            this.serverPosition = transform.position;
            this.isEnemyDead = false;
            this.enabled = true;

            enemyRayHit = default;
            addPlayerVelocityToDestination = 3f;

            // Position
            if (IsOwner)
            {
                base.SyncPositionToClients();
            }
            else if (agent != null)
            {
                SetClientCalculatingAI(false);
            }

            LineRendererUtil = new LineRendererUtil(6, this.transform);
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

            if (!NpcController.Npc.isClimbingLadder
                && (NpcController.Npc.inSpecialInteractAnimation || NpcController.Npc.enteringSpecialAnimation))
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
                if (!NpcController.Npc.isClimbingLadder && !NpcController.Npc.inSpecialInteractAnimation && !NpcController.Npc.enteringSpecialAnimation)
                {
                    NpcController.SetTurnBodyTowardsDirectionWithPosition(this.transform.position);
                }

                // Npc is following ai agent position that follows destination path
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
            if (isEnemyDead || NpcController.Npc.isPlayerDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            if (State == null)
            {
                State = new SearchingForPlayerState(this);
            }

            State.DoAI();

            CheckIfStuck();
        }

        public void OrderMoveToDestination()
        {
            if (agent.isActiveAndEnabled && !isEnemyDead && !NpcController.Npc.isPlayerDead)
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
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;
            Vector3 posTargetCamera = targetPlayer.gameplayCamera.transform.position;
            if (Vector3.Distance(posTargetCamera, thisInternCamera.position) < (float)range
                && !Physics.Linecast(thisInternCamera.position, posTargetCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                Vector3 to = posTargetCamera - thisInternCamera.position;
                if (Vector3.Angle(thisInternCamera.forward, to) < width
                    || (proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posTargetCamera) < (float)proximityAwareness))
                {
                    return targetPlayer;
                }
            }

            return null;
        }

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

                Vector3 posInternCamera = intern.gameplayCamera.transform.position;
                if (Vector3.Distance(posInternCamera, thisInternCamera.position) < (float)range
                    && !Physics.Linecast(thisInternCamera.position, posInternCamera, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = posInternCamera - thisInternCamera.position;
                    if (Vector3.Angle(thisInternCamera.forward, to) < width
                        || (proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posInternCamera) < (float)proximityAwareness))
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

            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = this.NpcController.Npc.gameplayCamera.transform;
            float num2 = 1000f;
            int num3 = -1;
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = instanceSOR.allPlayerScripts[i];
                if (!PlayerIsTargetable(player))
                {
                    continue;
                }

                Vector3 position = player.gameplayCamera.transform.position;
                if ((position - this.transform.position).sqrMagnitude > range * range)
                {
                    continue;
                }

                if (Physics.Linecast(NpcController.Npc.gameplayCamera.transform.position, position, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                Vector3 to = position - thisInternCamera.position;
                float num = Vector3.Distance(thisInternCamera.position, position);
                if ((Vector3.Angle(thisInternCamera.forward, to) < width || (proximityAwareness != -1 && num < (float)proximityAwareness)) && num < num2)
                {
                    num2 = num;
                    num3 = i;
                }
            }

            if (targetPlayer != null
                && num3 != -1
                && targetPlayer != instanceSOR.allPlayerScripts[num3]
                && bufferDistance > 0f
                && Mathf.Abs(num2 - Vector3.Distance(base.transform.position, targetPlayer.transform.position)) < bufferDistance)
            {
                return null;
            }

            if (num3 < 0)
            {
                return null;
            }

            mostOptimalDistance = num2;
            return instanceSOR.allPlayerScripts[num3];
        }

        public EnemyAI? CheckLOSForEnemy(float width = 45f, int range = 20, int proximityAwareness = -1)
        {
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

                Vector3 positionEnemy = spawnedEnemy.transform.position;
                Vector3 directionEnemyFromCamera = positionEnemy - thisInternCamera.position;
                float sqrDistanceToEnemy = directionEnemyFromCamera.sqrMagnitude;
                if (sqrDistanceToEnemy > range * range)
                {
                    continue;
                }

                // Obstructed
                if (Physics.Linecast(thisInternCamera.position, positionEnemy, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                // Fear range
                int? fearRange = GetFearRangeForEnemies(spawnedEnemy);
                if (!fearRange.HasValue
                    || sqrDistanceToEnemy > fearRange * fearRange)
                {
                    continue;
                }
                Plugin.Logger.LogDebug($"fear range {fearRange}");

                // Proximity awareness
                if (proximityAwareness > -1
                    && sqrDistanceToEnemy < (float)proximityAwareness * (float)proximityAwareness)
                {
                    Plugin.Logger.LogDebug($"{NpcController.Npc.playerUsername} DANGER CLOSE \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return instanceRM.SpawnedEnemies[index];
                }

                // Line of Sight
                if (Vector3.Angle(thisInternCamera.forward, directionEnemyFromCamera) < width)
                {
                    Plugin.Logger.LogDebug($"{NpcController.Npc.playerUsername} DANGER LOS \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return instanceRM.SpawnedEnemies[index];
                }
            }

            return null;
        }

        private int? GetFearRangeForEnemies(EnemyAI enemy)
        {
            Plugin.Logger.LogDebug($"\"{enemy.enemyType.enemyName}\" {enemy.enemyType.name}");
            switch (enemy.enemyType.enemyName)
            {
                case "Flowerman":
                case "Crawler":
                case "Centipede":
                case "Bunker Spider":
                case "Spring":
                case "MouthDog":
                case "ForestGiant":
                case "Butler Bees":
                    return 30;

                case "Nutcracker":
                case "Red Locust Bees":
                    return 10;

                case "Earth Leviathan":
                case "Blob":
                    return 5;

                case "Puffer":
                    return 2;

                case "Tulip Snake":
                    return 1;

                case "Butler":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30;
                    }
                    else
                    {
                        return null;
                    }

                case "Hoarding bug":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30;
                    }
                    else
                    {
                        return null;
                    }

                case "Jester":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30;
                    }
                    else
                    {
                        return null;
                    }

                case "RadMech":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 30;
                    }
                    else
                    {
                        return null;
                    }

                case "Baboon hawk":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 30;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    // "Docile Locust Bees"
                    // "Manticoil"
                    // "Masked"
                    // "Girl"
                    return null;
            }
        }

        public bool IsTargetInShipBoundsExpanded()
        {
            if (targetPlayer == null)
            {
                return false;
            }

            return targetPlayer.isInElevator || InternManager.Instance.GetExpandedShipBounds().Contains(targetPlayer.transform.position);
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
            if (!NpcController.HasToMove)
            {
                return;
            }

            if (NpcController.Npc.jetpackControls || NpcController.Npc.isClimbingLadder)
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
                if (!NpcController.IsJumping)
                {
                    bool canMoveCheckWhileJump = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                                NpcController.Npc.thisController.transform.position + new Vector3(0, 1.8f, 0),
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
                    bool canMoveCheckWhileCrouch = !RayUtil.RayCastForwardAndDraw(LineRendererUtil.GetLineRenderer(),
                                                                                  NpcController.Npc.thisController.transform.position + new Vector3(0, 1f, 0),
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

            // Check for hole
            if ((this.transform.position - NpcController.Npc.transform.position).sqrMagnitude > 2.5f * 2.5f)
            {
                // Ladders
                bool isUsingLadder = UseLadderIfNeeded();

                if (isUsingLadder)
                {
                    timeSinceStuck = 0f;
                    return;
                }

                if (Time.timeSinceLevelLoad - TimeSinceUsingEntrance > Const.WAIT_TIME_TO_TELEPORT)
                {
                    NpcController.Npc.transform.position = this.transform.position;
                    Log($"{NpcController.Npc.playerUsername}============ HOLE ???? dist {(this.transform.position - NpcController.Npc.transform.position).magnitude}");
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
        }

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
                    Log($"{NpcController.Npc.playerUsername} Wants to go up on ladder");
                    // Wants to go up on ladder
                    NpcController.OrderToGoUpDownLadder(hasToGoDown: false);
                    return ladder;
                }
                else if ((ladderTopPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
                {
                    Log($"{NpcController.Npc.playerUsername} Wants to go down on ladder");
                    // Wants to go down on ladder
                    NpcController.OrderToGoUpDownLadder(hasToGoDown: true);
                    return ladder;
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
                NpcController.OrderToStopMoving();
            }

            return true;
        }

        public bool AreHandsFree()
        {
            return HeldItem == null;
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

            if ((grabbableObject.transform.position - InternManager.Instance.ShipBoundClosestPoint(grabbableObject.transform.position)).sqrMagnitude
                    < Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT * Const.DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT)
            {
                return false;
            }

            if (DictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return false;
                }
            }
            TrimDictJustDroppedItems();

            if (this.PathIsIntersectedByLineOfSight(grabbableObject.transform.position, false, false))
            {
                Plugin.Logger.LogDebug($"object {grabbableObject.name} pathfind is not reachable");
                return false;
            }

            return true;
        }

        private static void TrimDictJustDroppedItems()
        {
            if (DictJustDroppedItems != null && DictJustDroppedItems.Count > 20)
            {
                Plugin.Logger.LogDebug($"TrimDictJustDroppedItems Count{DictJustDroppedItems.Count}");
                var itemsToClean = DictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS);
                foreach (var item in itemsToClean)
                {
                    DictJustDroppedItems.Remove(item.Key);
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

        private void TeleportAgentAndBody(Vector3 pos)
        {
            if ((this.transform.position - pos).sqrMagnitude < 1f * 1f)
            {
                return;
            }
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 5f, -1);
            serverPosition = navMeshPosition;
            NpcController.Npc.transform.position = navMeshPosition;

            if (IsOwner)
            {
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

        private bool IsClientOwnerOfIntern()
        {
            return this.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        #region TeleportIntern RPC

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
        private void TeleportInternServerRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            TeleportInternClientRpc(pos, setOutside, isUsingEntrance);
        }
        [ClientRpc]
        private void TeleportInternClientRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
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
                TimeSinceUsingEntrance = Time.timeSinceLevelLoad;
                EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
                if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
                {
                    entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                }
            }
        }

        #endregion

        #region AssignTargetAndSetMovingTo RPC

        public void SyncAssignTargetAndSetMovingTo(PlayerControllerB newTarget)
        {
            if (this.OwnerClientId != newTarget.actualClientId)
            {
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

        [ServerRpc(RequireOwnership = false)]
        private void SyncAssignTargetAndSetMovingToServerRpc(ulong playerid)
        {
            SyncFromAssignTargetAndSetMovingToClientRpc(playerid);
        }

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
            this.destination = RoundManager.Instance.GetNavMeshPosition(this.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
            if (this.State == null || this.State.GetAIState() != EnumAIStates.GetCloseToPlayer)
            {
                this.State = new GetCloseToPlayerState(this);
            }
        }

        #endregion

        #region UpdatePlayerPosition RPC

        public void SyncUpdatePlayerPosition(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            if (IsServer)
            {
                UpdatePlayerPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
            else
            {
                UpdatePlayerPositionServerRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
            }
        }

        [ServerRpc]
        private void UpdatePlayerPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
        {
            UpdatePlayerPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
        }

        [ClientRpc]
        private void UpdatePlayerPositionClientRpc(Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
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

        public void SyncUpdatePlayerRotationAndLook(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (IsServer)
            {
                UpdatePlayerRotationAndLookClientRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
            else
            {
                UpdatePlayerRotationAndLookServerRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
        }

        [ServerRpc]
        private void UpdatePlayerRotationAndLookServerRpc(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            UpdatePlayerRotationAndLookClientRpc(direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
        }

        [ClientRpc]
        private void UpdatePlayerRotationAndLookClientRpc(Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
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

        [ServerRpc]
        public void UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)
        {
            UpdatePlayerAnimationClientRpc(animationState, animationSpeed);
        }

        [ClientRpc]
        private void UpdatePlayerAnimationClientRpc(int animationState, float animationSpeed)
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

        public void UpdateSpecialAnimationValue(bool specialAnimation, short yVal, float timed, bool climbingLadder)
        {
            if (!IsClientOwnerOfIntern())
            {
                return;
            }
            IsInSpecialAnimation(specialAnimation, yVal, timed, climbingLadder);

            if (IsServer)
            {
                IsInSpecialAnimationClientRpc(specialAnimation, yVal, timed, climbingLadder);
            }
            else
            {
                IsInSpecialAnimationServerRpc(specialAnimation, yVal, timed, climbingLadder);
            }
        }

        [ServerRpc]
        private void IsInSpecialAnimationServerRpc(bool specialAnimation, short yVal, float timed, bool climbingLadderd)
        {
            IsInSpecialAnimationClientRpc(specialAnimation, yVal, timed, climbingLadderd);
        }

        [ClientRpc]
        private void IsInSpecialAnimationClientRpc(bool specialAnimation, short yVal, float timed, bool climbingLadder)
        {
            if (IsClientOwnerOfIntern())
            {
                return;
            }

            IsInSpecialAnimation(specialAnimation, yVal, timed, climbingLadder);
        }

        private void IsInSpecialAnimation(bool specialAnimation, short yVal, float timed, bool climbingLadder)
        {
            if (NpcController == null)
            {
                return;
            }

            PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch(NpcController.Npc, specialAnimation, timed, climbingLadder);
            NpcController.Npc.ResetZAndXRotation();
        }

        #endregion

        #region Grab item RPC

        [ServerRpc(RequireOwnership = false)]
        public void GrabItemServerRpc(NetworkObjectReference networkObjectReference)
        {
            GrabItemClientRpc(networkObjectReference);
        }

        [ClientRpc]
        private void GrabItemClientRpc(NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                Plugin.Logger.LogError($"GrabItem for InterAI {this.InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();

            if (this.HeldItem == grabbableObject)
            {
                Plugin.Logger.LogError($"Try to grab already held item {grabbableObject} on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabItem(grabbableObject);

            if (AreHandsFree())
            {
                // Problem with taking object
                ListInvalidObjects.Add(grabbableObject);
            }
        }

        private void GrabItem(GrabbableObject grabbableObject)
        {
            Plugin.Logger.LogInfo($"Try to grab item {grabbableObject} on client #{NetworkManager.LocalClientId}");
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
            if (grabbableObject.itemProperties.grabSFX != null)
            {
                NpcController.Npc.itemAudio.PlayOneShot(grabbableObject.itemProperties.grabSFX, 1f);
            }

            // animations
            NpcController.Npc.playerBodyAnimator.SetBool("GrabInvalidated", false);
            NpcController.Npc.playerBodyAnimator.SetBool("GrabValidated", false);
            NpcController.Npc.playerBodyAnimator.SetBool("cancelHolding", false);
            NpcController.Npc.playerBodyAnimator.ResetTrigger("Throw");
            this.SetSpecialGrabAnimationBool(true, grabbableObject);

            if (this.grabObjectCoroutine != null)
            {
                base.StopCoroutine(this.grabObjectCoroutine);
            }
            this.grabObjectCoroutine = base.StartCoroutine(this.GrabAnimationCoroutine());
        }

        private IEnumerator GrabAnimationCoroutine()
        {
            yield return new WaitForSeconds(this.HeldItem.itemProperties.grabAnimationTime);
            this.SetSpecialGrabAnimationBool(false, this.HeldItem);
            yield break;
        }

        private void SetSpecialGrabAnimationBool(bool setBool, GrabbableObject currentItem)
        {
            NpcController.Npc.playerBodyAnimator.SetBool("Grab", setBool);
            if (!string.IsNullOrEmpty(currentItem.itemProperties.grabAnim))
            {
                try
                {
                    NpcController.Npc.playerBodyAnimator.SetBool(currentItem.itemProperties.grabAnim, setBool);
                }
                catch (Exception)
                {
                    Plugin.Logger.LogError("An item tried to set an animator bool which does not exist: " + currentItem.itemProperties.grabAnim);
                }
            }
        }

        #endregion

        #region Drop item RPC

        [ServerRpc(RequireOwnership = false)]
        public void DropItemServerRpc()
        {
            DropItemClientRpc();
        }

        [ClientRpc]
        private void DropItemClientRpc()
        {
            DropItem();
        }

        private void DropItem()
        {
            Plugin.Logger.LogInfo($"Try to drop item on client #{NetworkManager.LocalClientId}");
            if (this.HeldItem == null)
            {
                Plugin.Logger.LogError($"Try to drop not held item on client #{NetworkManager.LocalClientId}");
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
            NpcController.Npc.playerBodyAnimator.SetBool("cancelHolding", true);
            NpcController.Npc.playerBodyAnimator.SetTrigger("Throw");

            Plugin.Logger.LogDebug($"intern dropped {grabbableObject}");
            DictJustDroppedItems[grabbableObject] = Time.realtimeSinceStartup;
            this.HeldItem = null;
            NpcController.Npc.isHoldingObject = false;
            NpcController.Npc.twoHanded = false;
            NpcController.Npc.twoHandedAnimation = false;
            NpcController.Npc.carryWeight -= Mathf.Clamp(grabbableObject.itemProperties.weight - 1f, 0f, 10f);
        }

        #endregion

        #region Damage intern from client players RPC

        [ServerRpc(RequireOwnership = false)]
        public void DamageInternFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClientClientRpc(damageAmount, hitDirection, playerWhoHit);
        }

        [ClientRpc]
        private void DamageInternFromOtherClientClientRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClient(damageAmount, hitDirection, playerWhoHit);
        }

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

        public void SyncDamageIntern(int damageNumber,
                                     CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                     int deathAnimation = 0,
                                     bool fallDamage = false,
                                     Vector3 force = default)
        {
            Plugin.Logger.LogDebug($"SyncDamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
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

        [ServerRpc]
        private void DamageInternServerRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageInternClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        [ClientRpc]
        private void DamageInternClientRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        private void DamageIntern(int damageNumber,
                                  CauseOfDeath causeOfDeath,
                                  int deathAnimation,
                                  bool fallDamage,
                                  Vector3 force)
        {
            Plugin.Logger.LogDebug($"DamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

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
            Plugin.Logger.LogDebug($"intern health {NpcController.Npc.health}, damage : {damageNumber}");

            if (NpcController.Npc.health <= 0)
            {
                if (IsClientOwnerOfIntern())
                {
                    KillInternSpawnBodyServerRpc(true);
                }
                this.KillIntern(force, true, causeOfDeath, deathAnimation);
            }
            else
            {
                if (NpcController.Npc.health < 10
                    && !NpcController.Npc.criticallyInjured)
                {
                    MakeCriticallyInjured();
                }
                else
                {
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
                NpcController.Npc.playerBodyAnimator.SetTrigger("Damage");
            }
            NpcController.Npc.specialAnimationWeight = 1f;
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);
        }

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

        [ServerRpc]
        private void MakeCriticallyInjuredServerRpc()
        {
            MakeCriticallyInjuredClientRpc();
        }

        [ClientRpc]
        private void MakeCriticallyInjuredClientRpc()
        {
            MakeCriticallyInjured();
        }

        private void MakeCriticallyInjured()
        {
            NpcController.Npc.bleedingHeavily = true;
            NpcController.Npc.criticallyInjured = true;
            NpcController.Npc.hasBeenCriticallyInjured = true;
        }

        [ServerRpc]
        private void HealServerRpc()
        {
            HealClientRpc();
        }

        [ClientRpc]
        private void HealClientRpc()
        {
            Heal();
        }

        private void Heal()
        {
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.criticallyInjured = false;
        }

        #endregion

        #region Kill player RPC

        public void SyncKillIntern(Vector3 bodyVelocity,
                                   bool spawnBody = true,
                                   CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                   int deathAnimation = 0)
        {
            Plugin.Logger.LogDebug($"SyncKillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
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
                KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
            }
            else
            {
                KillInternServerRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
            }
        }

        [ServerRpc]
        private void KillInternServerRpc(Vector3 bodyVelocity,
                                         bool spawnBody,
                                         CauseOfDeath causeOfDeath,
                                         int deathAnimation)
        {
            KillInternSpawnBody(spawnBody);
            KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
        }

        [ServerRpc]
        private void KillInternSpawnBodyServerRpc(bool spawnBody)
        {
            KillInternSpawnBody(spawnBody);
        }

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

        [ClientRpc]
        private void KillInternClientRpc(Vector3 bodyVelocity,
                                                 bool spawnBody,
                                                 CauseOfDeath causeOfDeath,
                                                 int deathAnimation)
        {
            KillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
        }

        private void KillIntern(Vector3 bodyVelocity,
                                bool spawnBody,
                                CauseOfDeath causeOfDeath,
                                int deathAnimation)
        {
            Plugin.Logger.LogDebug($"KillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

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
            Plugin.Logger.LogDebug($"Running kill intern function for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{this.InternId}");
            if (spawnBody)
            {
                NpcController.Npc.SpawnDeadBody((int)NpcController.Npc.playerClientId, bodyVelocity, (int)causeOfDeath, NpcController.Npc, deathAnimation, null);
            }
            if (this.HeldItem != null)
            {
                this.DropItem();
            }
            NpcController.Npc.DisableJetpackControlsLocally();
        }

        #endregion

        #region Jump RPC

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

        [ServerRpc]
        private void JumpServerRpc()
        {
            JumpClientRpc();
        }

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

        [ServerRpc]
        private void JumpLandFromServerRpc(bool fallHard)
        {
            JumpLandFromClientRpc(fallHard);
        }

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

        [ServerRpc]
        private void ChangeSinkingStateServerRpc(bool startSinking, float sinkingSpeed, int audioClipIndex)
        {
            ChangeSinkingStateClientRpc(startSinking, sinkingSpeed, audioClipIndex);
        }

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
                //if (this.currentVoiceChatIngameSettings != null)
                //{
                //    this.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
                //}
            }
        }

        #endregion

        #region Disable Jetpack RPC

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

        [ServerRpc]
        private void DisableJetpackModeServerRpc()
        {
            DisableJetpackModeClientRpc();
        }

        [ClientRpc]
        private void DisableJetpackModeClientRpc()
        {
            NpcController.Npc.DisableJetpackControlsLocally();
        }

        #endregion

        #region Stop performing emote RPC

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

        [ServerRpc]
        private void StopPerformingEmoteServerRpc()
        {
            StopPerformingEmoteClientRpc();
        }

        [ClientRpc]
        private void StopPerformingEmoteClientRpc()
        {
            NpcController.Npc.performingEmote = false;
        }

        #endregion
    }
}