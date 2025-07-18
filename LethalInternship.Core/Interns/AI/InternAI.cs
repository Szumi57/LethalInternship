using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.BT;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks;
using LethalInternship.SharedAbstractions.Hooks.LethalMinHooks;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Component = UnityEngine.Component;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.Core.Interns.AI
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
    public class InternAI : EnemyAI, IInternAI
    {
        public INpcController NpcController => npcController;
        public PlayerControllerB Npc => npcController.Npc;
        public IInternIdentity InternIdentity { get => internIdentity; set => internIdentity = value; }
        public new ulong OwnerClientId => base.OwnerClientId;
        public new NetworkObject NetworkObject => base.NetworkObject;
        public Transform Transform => this.transform;
        public IRagdollInternBody RagdollInternBody { get => ragdollInternBody; set => ragdollInternBody = value; }
        public bool IsEnemyDead => base.isEnemyDead;
        public new bool IsSpawned => base.IsSpawned;
        public bool AnimationCoroutineRagdollingRunning => animationCoroutineRagdollingRunning;
        public List<Component> ListModelReplacement { get => listModelReplacement; set => listModelReplacement = value; }
        public GrabbableObject? HeldItem { get => heldItem; set => heldItem = value; }

        private INpcController npcController = null!;
        private IInternIdentity internIdentity = null!;
        private IRagdollInternBody ragdollInternBody = null!;

        /// <summary>
        /// Dictionnary of the recently dropped object on the ground.
        /// The intern will not try to grab them for a certain time (<see cref="Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS"><c>Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS</c></see>).
        /// </summary>
        public static Dictionary<GrabbableObject, float> DictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        public BTController BTController = null!;

        public EnumCommandTypes CurrentCommand;
        public EnemyAI? CurrentEnemy;
        public GrabbableObject? TargetItem;
        public Vector3? TargetLastKnownPosition;
        public EntranceTeleport? ClosestEntrance;
        public Vector3 NextPos;
        public bool CanRun = true;

        public IPointOfInterest? PointOfInterest = null!;

        public AudioSource InternVoice = null!;
        /// <summary>
        /// Currently held item by intern
        /// </summary>
        public Collider InternBodyCollider = null!;
        private GrabbableObject? heldItem = null!;

        public int InternId = -1;
        public int MaxHealth = ConfigConst.DEFAULT_INTERN_MAX_HEALTH;
        public float TimeSinceTeleporting = 0f;

        private List<Component> listModelReplacement = null!;
        public EntranceTeleport[] EntrancesTeleportArray = null!;

        public TimedTouchingGroundCheck IsTouchingGroundTimedCheck = null!;
        public TimedAngleFOVWithLocalPlayerCheck AngleFOVWithLocalPlayerTimedCheck = null!;

        public LineRendererUtil LineRendererUtil = null!;

        private EnumStateControllerMovement StateControllerMovement;
        private InteractTrigger[] laddersInteractTrigger = null!;
        private DoorLock[] doorLocksArray = null!;
        private Dictionary<string, Component> dictComponentByCollider = null!;

        private DeadBodyInfo ragdollBodyDeadBodyInfo = null!;

        private Coroutine grabObjectCoroutine = null!;
        private Coroutine? spawnAnimationCoroutine = null!;
        private bool animationCoroutineRagdollingRunning = false;

        private string stateIndicatorServer = string.Empty;
        private Vector3 previousWantedDestination;
        private float updateDestinationIntervalInternAI;
        private float healthRegenerateTimerMax;
        private float timerCheckDoor;

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

        public void AdaptController(PlayerControllerB playerControllerB)
        {
            npcController = new NpcController(playerControllerB);
        }

        /// <summary>
        /// Start unity method.
        /// </summary>
        /// <remarks>
        /// The agent is initialized here
        /// </remarks>
        public override void Start()
        {
            // AIIntervalTime
            AIIntervalTime = 0.3f;

            try
            {
                // Ok so the unity project is so broken with this dll right now
                // External mod dependencies chain nightmare
                // So we initialize enemyType in the code, it's ugly sorry (but it works ;p)
                EnemyType enemyTypeIntern = ScriptableObject.CreateInstance<EnemyType>();
                enemyTypeIntern.name = "InternNPC";
                enemyTypeIntern.enemyName = enemyTypeIntern.name;
                enemyTypeIntern.doorSpeedMultiplier = 0.25f;
                enemyTypeIntern.canDie = true;
                this.enemyType = enemyTypeIntern;

                gameObject.GetComponentInChildren<EnemyAICollisionDetect>().mainScript = this;
                agent = gameObject.GetComponentInChildren<NavMeshAgent>();
                agent.enabled = false;
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
                PluginLoggerHook.LogError?.Invoke(string.Format("Error when initializing intern variables for {0} : {1}", gameObject.name, arg));
            }
        }

        /// <summary>
        /// Initialization of the field.
        /// </summary>
        /// <remarks>
        /// This method is used as an initialization and re-initialization too.
        /// </remarks>
        public void Init(EnumSpawnAnimation enumSpawnAnimation)
        {
            // Entrances
            EntrancesTeleportArray = FindObjectsOfType<EntranceTeleport>(includeInactive: false);

            // Doors
            doorLocksArray = FindObjectsOfType<DoorLock>(includeInactive: false);

            // Important colliders
            InitImportantColliders();

            // Model replacements
            listModelReplacement = new List<Component>();

            // Grabbableobject
            InternManager.Instance.RegisterItems();

            // Init controller
            NpcController.Awake();

            // Refresh billboard position
            StartCoroutine(WaitSecondsForChangeSuitToApply());

            // Health
            MaxHealth = InternIdentity.HpMax;
            NpcController.Npc.health = MaxHealth;
            healthRegenerateTimerMax = 100f / MaxHealth;
            NpcController.Npc.healthRegenerateTimer = healthRegenerateTimerMax;

            // AI init
            ventAnimationFinished = true;
            isEnemyDead = false;
            enabled = true;
            addPlayerVelocityToDestination = 3f;

            // Behavior tree
            BTController = new BTController(this);

            // Body collider
            InternBodyCollider = NpcController.Npc.GetComponentInChildren<Collider>();

            // Intern voice
            InitInternVoiceComponent();
            UpdateInternVoiceEffects();

            // Line renderer used for debugging stuff
            LineRendererUtil = new LineRendererUtil(6, transform);

            TeleportAgentAIAndBody(NpcController.Npc.transform.position);
            StateControllerMovement = EnumStateControllerMovement.FollowAgent;

            // Start timed calculation
            IsTouchingGroundTimedCheck = new TimedTouchingGroundCheck();
            AngleFOVWithLocalPlayerTimedCheck = new TimedAngleFOVWithLocalPlayerCheck();

            // Spawn animation
            spawnAnimationCoroutine = BeginInternSpawnAnimation(enumSpawnAnimation);
        }

        private void InitInternVoiceComponent()
        {
            if (creatureVoice == null)
            {
                foreach (var component in gameObject.GetComponentsInChildren<AudioSource>())
                {
                    if (component.name == "CreatureVoice")
                    {
                        creatureVoice = component;
                        break;
                    }
                }
            }
            if (creatureVoice == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"Could not initialize intern {InternId} {NpcController.Npc.playerUsername} voice !");
                return;
            }

            NpcController.Npc.currentVoiceChatAudioSource = creatureVoice;
            InternVoice = NpcController.Npc.currentVoiceChatAudioSource;
            InternVoice.enabled = true;
            InternIdentity.Voice.InternID = InternId;
            InternIdentity.Voice.CurrentAudioSource = InternVoice;

            // OccludeAudio
            NpcController.OccludeAudioComponent = creatureVoice.GetComponent<OccludeAudio>();

            // AudioLowPassFilter
            AudioLowPassFilter? audioLowPassFilter = creatureVoice.GetComponent<AudioLowPassFilter>();
            if (audioLowPassFilter == null)
            {
                audioLowPassFilter = creatureVoice.gameObject.AddComponent<AudioLowPassFilter>();
            }
            NpcController.AudioLowPassFilterComponent = audioLowPassFilter;

            // AudioHighPassFilter
            AudioHighPassFilter? audioHighPassFilter = creatureVoice.GetComponent<AudioHighPassFilter>();
            if (audioHighPassFilter == null)
            {
                audioHighPassFilter = creatureVoice.gameObject.AddComponent<AudioHighPassFilter>();
            }
            NpcController.AudioHighPassFilterComponent = audioHighPassFilter;

            // AudioMixerGroup
            if ((int)NpcController.Npc.playerClientId >= SoundManager.Instance.playerVoiceMixers.Length)
            {
                // Because of morecompany, playerVoiceMixers gets somehow resized down
                InternManager.Instance.ResizePlayerVoiceMixers(InternManager.Instance.AllEntitiesCount);
            }
            InternVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[(int)NpcController.Npc.playerClientId];
        }

        private void FixedUpdate()
        {
            if (NpcController == null)
            {
                // Intern AI not init
                return;
            }

            UpdateSurfaceRayCast();
        }

        private void UpdateSurfaceRayCast()
        {
            NpcController.IsTouchingGround = IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position);

            // Update current material standing on
            if (NpcController.IsTouchingGround)
            {
                RaycastHit groundRaycastHit = IsTouchingGroundTimedCheck.GetGroundHit(NpcController.Npc.thisPlayerBody.position);
                if (InternManager.Instance.DictTagSurfaceIndex.ContainsKey(groundRaycastHit.collider.tag))
                {
                    NpcController.Npc.currentFootstepSurfaceIndex = InternManager.Instance.DictTagSurfaceIndex[groundRaycastHit.collider.tag];
                }
            }
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
            if (NpcController == null)
            {
                // Intern AI not init
                return;
            }

            // Update identity
            InternIdentity.Hp = NpcController.Npc.isPlayerDead ? 0 : NpcController.Npc.health;

            // Not owner no AI
            if (!IsOwner)
            {
                if (currentSearch.inProgress)
                {
                    StopSearch(currentSearch);
                }

                SetAgent(enabled: false);

                return;
            }

            if (!NpcController.Npc.gameObject.activeSelf
                || !NpcController.Npc.isPlayerControlled
                || isEnemyDead
                || NpcController.Npc.isPlayerDead)
            {
                // Intern dead or
                // Not controlled we do nothing
                SetAgent(enabled: false);
                return;
            }

            // No AI calculation if in special animation
            if (inSpecialAnimation
                || (RagdollInternBody != null && RagdollInternBody.IsRagdollBodyHeld()))
            {
                SetAgent(enabled: false);
                return;
            }

            // No AI calculation if in special animation if climbing ladder or inSpecialInteractAnimation
            if (!NpcController.Npc.isClimbingLadder
                && (NpcController.Npc.inSpecialInteractAnimation || NpcController.Npc.enteringSpecialAnimation))
            {
                SetAgent(enabled: false);
                return;
            }

            // Update movement
            float x;
            float z;
            if (NpcController.HasToMove)
            {
                Vector2 vector2 = new Vector2(NpcController.MoveVector.x, NpcController.MoveVector.z);
                agent.speed = 1f * vector2.magnitude;

                if (!NpcController.Npc.isClimbingLadder
                    && !NpcController.Npc.inSpecialInteractAnimation
                    && !NpcController.Npc.enteringSpecialAnimation)
                {
                    // Npc is following ai agent position that follows destination path
                    NpcController.SetTurnBodyTowardsDirectionWithPosition(transform.position);
                }

                x = Mathf.Lerp(NpcController.Npc.transform.position.x, transform.position.x, 0.075f);
                z = Mathf.Lerp(NpcController.Npc.transform.position.z, transform.position.z, 0.075f);
            }
            else
            {
                SetAgent(enabled: false);
                x = Mathf.Lerp(NpcController.Npc.transform.position.x + NpcController.MoveVector.x * Time.deltaTime, transform.position.x, 0.075f);
                z = Mathf.Lerp(NpcController.Npc.transform.position.z + NpcController.MoveVector.z * Time.deltaTime, transform.position.z, 0.075f);
            }

            // Movement free (falling from bridge, jetpack, tulip snake taking off...)
            bool shouldFreeMovement = ShouldFreeMovement();

            // Update position
            if (shouldFreeMovement
                || StateControllerMovement == EnumStateControllerMovement.Free)
            {
                StateControllerMovement = EnumStateControllerMovement.Free;
                //PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} falling ! NpcController.Npc.transform.position {NpcController.Npc.transform.position} MoveVector {NpcController.MoveVector}");
                NpcController.Npc.transform.position = NpcController.Npc.transform.position + NpcController.MoveVector * Time.deltaTime;
            }
            else if (StateControllerMovement == EnumStateControllerMovement.FollowAgent)
            {
                Vector3 aiPosition = transform.position;
                NpcController.Npc.transform.position = new Vector3(x,
                                                                   aiPosition.y,
                                                                   z); ;
                transform.position = aiPosition;
                NpcController.Npc.ResetFallGravity();
            }

            // Is still falling ?
            if (StateControllerMovement == EnumStateControllerMovement.Free
                && IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position)
                && !shouldFreeMovement)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} ============= touch ground GroundHit.point {NpcController.GroundHit.point}");
                StateControllerMovement = EnumStateControllerMovement.FollowAgent;
                TeleportAgentAIAndBody(IsTouchingGroundTimedCheck.GetGroundHit(NpcController.Npc.thisPlayerBody.position).point);
                //PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} ============= NpcController.Npc.transform.position {NpcController.Npc.transform.position}");
            }

            // No AI when falling
            if (StateControllerMovement == EnumStateControllerMovement.Free)
            {
                return;
            }

            // Update interval timer for AI calculation
            if (updateDestinationIntervalInternAI >= 0f)
            {
                updateDestinationIntervalInternAI -= Time.deltaTime;
            }
            else
            {
                SetAgent(enabled: true);

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
            if (isEnemyDead
                || NpcController.Npc.isPlayerDead
                || (RagdollInternBody != null && RagdollInternBody.IsRagdollBodyHeld()))
            {
                return;
            }

            // Do the AI calculation behaviour for the current state
            //State.DoAI();

            BTController.TickTree(AIIntervalTime);

            // Doors
            OpenDoorIfNeeded();
        }

        #region Commands

        public IPointOfInterest? GetPointOfInterest()
        {
            return this.PointOfInterest;
        }

        public void SetCommandTo(EnumCommandTypes commandType)
        {
            CurrentCommand = commandType;
        }

        public void SetCommandToGoToPosition(IPointOfInterest pointOfInterest)
        {
            this.PointOfInterest = pointOfInterest;
            CurrentCommand = EnumCommandTypes.GoToPosition;

            NavMesh.CalculatePath(this.transform.position, this.PointOfInterest.GetPoint(), NavMesh.AllAreas, this.path1);
            if (this.path1.status == NavMeshPathStatus.PathPartial)
            {
                NextPos = path1.corners[path1.corners.Length - 1];
            }
            else
            {
                NextPos = this.PointOfInterest.GetPoint();
            }

            PluginLoggerHook.LogDebug?.Invoke($"SetCommandToGoToPosition {this.PointOfInterest.GetPoint()}, nextpos {NextPos}");
        }

        public void SetCommandToFollowPlayer()
        {
            CurrentCommand = EnumCommandTypes.FollowPlayer;
            this.PointOfInterest = null;
            PluginLoggerHook.LogDebug?.Invoke($"SetCommandToFollowPlayer");
        }

        #endregion

        public void UpdateController()
        {
            if (RagdollInternBody != null
                && RagdollInternBody.IsRagdollBodyHeld())
            {
                return;
            }

            if (NpcController.IsControllerInCruiser)
            {
                return;
            }

            NpcController.Update();
        }

        private void LateUpdate()
        {
            if (NpcController == null)
            {
                // Intern AI not init
                return;
            }

            // Update voice position
            InternVoice.transform.position = NpcController.Npc.gameplayCamera.transform.position;
        }

        private bool ShouldFreeMovement()
        {
            if (IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position)
                && dictComponentByCollider.TryGetValue(IsTouchingGroundTimedCheck.GetGroundHit(NpcController.Npc.thisPlayerBody.position).collider.name, out Component component))
            {
                BridgeTrigger? bridgeTrigger = component as BridgeTrigger;
                if (bridgeTrigger != null
                    && bridgeTrigger.fallenBridgeColliders.Length > 0
                    && bridgeTrigger.fallenBridgeColliders[0].enabled)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} on fallen bridge ! {IsTouchingGroundTimedCheck.GetGroundHit(NpcController.Npc.thisPlayerBody.position).collider.name}");
                    return true;
                }
            }

            if (NpcController.Npc.externalForces.y > 7.1f)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} externalForces.y {NpcController.Npc.externalForces.y}");
                return true;
            }

            return false;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerControllerB componentPlayer = other.GetComponent<PlayerControllerB>();
                if (componentPlayer != null
                    && !InternManager.Instance.IsPlayerIntern(componentPlayer))
                {
                    NpcController.NearEntitiesPushVector += Vector3.Normalize((NpcController.Npc.transform.position - other.transform.position) * 100f) * 1.2f;
                }
            }
        }

        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)
        {
            if (!IsOwner)
            {
                return;
            }

            if (collidedEnemy == null
                || collidedEnemy.GetType() == typeof(InternAI)
                || collidedEnemy.GetType() == typeof(FlowerSnakeEnemy))
            {
                return;
            }

            if ((NpcController.Npc.transform.position - other.transform.position).sqrMagnitude < collidedEnemy.enemyType.pushPlayerDistance * collidedEnemy.enemyType.pushPlayerDistance)
            {
                NpcController.NearEntitiesPushVector += Vector3.Normalize((NpcController.Npc.transform.position - other.transform.position) * 100f) * collidedEnemy.enemyType.pushPlayerForce;
            }

            // Enemy collide with the intern collider
            collidedEnemy.OnCollideWithPlayer(InternBodyCollider);
        }

        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
        {
            if (NpcController == null
                || !NpcController.Npc.gameObject.activeSelf
                || !NpcController.Npc.isPlayerControlled
                || isEnemyDead
                || NpcController.Npc.isPlayerDead)
            {
                return;
            }

            // Player voice = 75 ?
            if (noiseID != 75)
            {
                return;
            }

            // Make the intern stop talking for some time
            InternIdentity.Voice.TryStopAudioFadeOut();

            if (IsOwner)
            {
                InternIdentity.Voice.SetNewRandomCooldownAudio();
            }

            PluginLoggerHook.LogDebug?.Invoke($"Intern {NpcController.Npc.playerUsername} detected noise noisePosition {noisePosition}, noiseLoudness {noiseLoudness}, timesPlayedInOneSpot {timesPlayedInOneSpot}, noiseID {noiseID}");

            // Player heard
            // todo : BT
        }

        public void SetAgent(bool enabled)
        {
            if (agent != null)
            {
                agent.enabled = enabled;
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
                destination = position;
            }
        }

        /// <summary>
        /// Try to set the destination on the agent, if destination not reachable, try the closest possible position of the destination
        /// </summary>
        public void UpdateDestinationToAgent()
        {
            if (agent.isActiveAndEnabled
                && agent.isOnNavMesh
                && !isEnemyDead
                && !NpcController.Npc.isPlayerDead
                && !StartOfRound.Instance.shipIsLeaving)
            {
                TrySetDestinationToPosition(destination);
                agent.SetDestination(destination);
            }
        }

        public void OrderAgentAndBodyMoveToDestination()
        {
            NpcController.OrderToMove();
            UpdateDestinationToAgent();
        }

        public bool TrySetDestinationToPosition(Vector3 position, bool checkForPath = false)
        {
            NavMesh.CalculatePath(this.transform.position, position, NavMesh.AllAreas, this.path1);
            if (this.path1.status == NavMeshPathStatus.PathPartial)
            {
                PluginLoggerHook.LogDebug?.Invoke($"TrySetDestinationToPosition CalculatePath {this.path1.status}");
                return SetDestinationToPosition(path1.corners[path1.corners.Length - 1]);
            }
            return SetDestinationToPosition(position);
        }

        public void StopMoving()
        {
            if (NpcController.HasToMove)
            {
                NpcController.OrderToStopMoving();
            }
        }

        /// <summary>
        /// Is the current client running the code is the owner of the <c>InternAI</c> ?
        /// </summary>
        /// <returns></returns>
        public bool IsClientOwnerOfIntern()
        {
            return OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        private int MaxHealthPercent(int percentage)
        {
            return InternManager.Instance.MaxHealthPercent(percentage, MaxHealth);
        }

        public void CheckAndBringCloserTeleportIntern(float percentageOfDestination)
        {
            bool isAPlayerSeeingIntern = false;
            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
            PlayerControllerB player;
            Vector3 vectorPlayerToIntern;
            Vector3 internDestination = NpcController.Npc.thisPlayerBody.transform.position + (destination - NpcController.Npc.transform.position) * percentageOfDestination;
            Vector3 internBodyDestination = internDestination + new Vector3(0, 1f, 0);
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                player = instanceSOR.allPlayerScripts[i];
                if (player.isPlayerDead
                    || !player.isPlayerControlled)
                {
                    continue;
                }

                // No obsruction
                if (!Physics.Linecast(player.gameplayCamera.transform.position, thisInternCamera.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    vectorPlayerToIntern = thisInternCamera.position - player.gameplayCamera.transform.position;
                    if (Vector3.Angle(player.gameplayCamera.transform.forward, vectorPlayerToIntern) < player.gameplayCamera.fieldOfView)
                    {
                        isAPlayerSeeingIntern = true;
                        break;
                    }
                }

                if (!Physics.Linecast(player.gameplayCamera.transform.position, internBodyDestination, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    vectorPlayerToIntern = internBodyDestination - player.gameplayCamera.transform.position;
                    if (Vector3.Angle(player.gameplayCamera.transform.forward, vectorPlayerToIntern) < player.gameplayCamera.fieldOfView)
                    {
                        isAPlayerSeeingIntern = true;
                        break;
                    }
                }
            }

            if (!isAPlayerSeeingIntern)
            {
                TeleportIntern(internDestination);
            }
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
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
            Vector3 posTargetCamera = targetPlayer.gameplayCamera.transform.position;
            if (Vector3.Distance(posTargetCamera, thisInternCamera.position) < range
                && !Physics.Linecast(thisInternCamera.position, posTargetCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                // Target close enough and nothing in between to break line of sight 
                Vector3 to = posTargetCamera - thisInternCamera.position;
                if (Vector3.Angle(thisInternCamera.forward, to) < width
                    || proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posTargetCamera) < proximityAwareness)
                {
                    // Target in FOV or proximity awareness range
                    return targetPlayer;
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
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
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
                if ((cameraPlayerPosition - transform.position).sqrMagnitude > range * range)
                {
                    continue;
                }

                if (!PlayerIsTargetable(player))
                {
                    continue;
                }

                // Nothing in between to break line of sight ?
                if (Physics.Linecast(thisInternCamera.position, cameraPlayerPosition, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                Vector3 vectorInternToPlayer = cameraPlayerPosition - thisInternCamera.position;
                float distanceInternToPlayer = Vector3.Distance(thisInternCamera.position, cameraPlayerPosition);
                if ((Vector3.Angle(thisInternCamera.forward, vectorInternToPlayer) < width || proximityAwareness != -1 && distanceInternToPlayer < proximityAwareness)
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
                && Mathf.Abs(currentClosestDistance - Vector3.Distance(transform.position, targetPlayer.transform.position)) < bufferDistance)
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
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
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

                // Obstructed
                if (Physics.Linecast(thisInternCamera.position, positionEnemy, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                // Fear range
                float? fearRange = GetFearRangeForEnemies(spawnedEnemy);
                if (!fearRange.HasValue)
                {
                    continue;
                }

                if (sqrDistanceToEnemy > fearRange * fearRange)
                {
                    continue;
                }
                // Enemy in distance of fear range

                // Proximity awareness, danger
                if (proximityAwareness > -1
                    && sqrDistanceToEnemy < proximityAwareness * (float)proximityAwareness)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} DANGER CLOSE \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return instanceRM.SpawnedEnemies[index];
                }

                // Line of Sight, danger
                if (Vector3.Angle(thisInternCamera.forward, directionEnemyFromCamera) < width)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} DANGER LOS \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
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
        public float? GetFearRangeForEnemies(EnemyAI enemy)
        {
            //PluginLoggerHook.LogDebug?.Invoke($"enemy \"{enemy.enemyType.enemyName}\" {enemy.enemyType.name}");
            switch (enemy.enemyType.enemyName)
            {
                case "Crawler":
                case "MouthDog":
                case "ForestGiant":
                case "Butler Bees":
                case "Nutcracker":
                case "Blob":
                case "ImmortalSnail":
                    return 15f;

                case "Red Locust Bees":
                case "Earth Leviathan":
                case "Clay Surgeon":
                case "Flowerman":
                case "Bush Wolf":
                    return 5f;

                case "Puffer":
                    return 2f;

                case "Centipede":
                    return 1f;

                case "Bunker Spider":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Spring":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Butler":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Hoarding bug":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Jester":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "RadMech":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Baboon hawk":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }


                case "Maneater":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
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
            NpcController.ReParentNotSpawnedTransform(newParent);
        }

        public string GetSizedBillboardStateIndicator()
        {
            string indicator;
            int sizePercentage = Math.Clamp((int)(100f + 2.5f * NpcController.GetSqrDistanceWithLocalPlayer(NpcController.Npc.transform.position)),
                                 100, 500);

            if (IsOwner)
            {
                //indicator = State == null ? string.Empty : State.GetBillboardStateIndicator();
                indicator = string.Empty;
            }
            else
            {
                indicator = stateIndicatorServer;
            }

            return $"<size={sizePercentage}%>{indicator}</size>";
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
            //InteractTrigger ladder;
            //Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            //for (int i = 0; i < laddersInteractTrigger.Length; i++)
            //{
            //    ladder = laddersInteractTrigger[i];
            //    Vector3 ladderBottomPos = ladder.bottomOfLadderPosition.position;
            //    Vector3 ladderTopPos = ladder.topOfLadderPosition.position;

            //    if ((ladderBottomPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
            //    {
            //        PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Wants to go up on ladder");
            //        // Wants to go up on ladder
            //        NpcController.OrderToGoUpDownLadder(hasToGoDown: false);
            //        return ladder;
            //    }
            //    else if ((ladderTopPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
            //    {
            //        PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Wants to go down on ladder");
            //        // Wants to go down on ladder
            //        NpcController.OrderToGoUpDownLadder(hasToGoDown: true);
            //        return ladder;
            //    }
            //}
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
                    Physics.IgnoreCollision(NpcController.Npc.playerCollider, door.GetComponent<Collider>());

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
            //if (NpcController.Npc.isClimbingLadder)
            //{
            //    return true;
            //}

            //InteractTrigger? ladder = GetLadderIfWantsToUseLadder();
            //if (ladder == null)
            //{
            //    return false;
            //}

            //// Intern wants to use ladder
            //// Removing all that for the moment
            ////if (PluginRuntimeProvider.Context.Config.TeleportWhenUsingLadders.Value)
            ////{
            ////    NpcController.Npc.transform.position = this.transform.position;
            ////    return true;
            ////}

            //// Try to use ladder
            //if (NpcController.CanUseLadder(ladder))
            //{
            //    InteractTriggerPatch.Interact_ReversePatch(ladder, NpcController.Npc.thisPlayerBody);

            //    // Set rotation of intern to face ladder
            //    NpcController.Npc.transform.rotation = ladder.ladderPlayerPositionNode.transform.rotation;
            //    NpcController.SetTurnBodyTowardsDirection(NpcController.Npc.transform.forward);
            //}
            //else
            //{
            //    // Wait to use ladder
            //    StopMoving();
            //}

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
                float sqrDistanceEyeGameObject = (gameObjectPosition - eye.position).sqrMagnitude;
                if (sqrDistanceEyeGameObject > Const.INTERN_OBJECT_RANGE * Const.INTERN_OBJECT_RANGE)
                {
                    continue;
                }

                // Black listed ? 
                if (IsGrabbableObjectBlackListed(gameObject))
                {
                    continue;
                }

                // Get grabbable object infos
                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    continue;
                }

                // Object on ship
                if (grabbableObject.isInElevator
                    || grabbableObject.isInShipRoom)
                {
                    continue;
                }

                // Object in cruiser vehicle
                if (grabbableObject.transform.parent != null
                    && grabbableObject.transform.parent.name.StartsWith("CompanyCruiser"))
                {
                    continue;
                }

                // Object in a container mod of some sort ?
                if (PluginRuntimeProvider.Context.IsModCustomItemBehaviourLibraryLoaded)
                {
                    if (CustomItemBehaviourLibraryHook.IsGrabbableObjectInContainerMod?.Invoke(grabbableObject) ?? false)
                    {
                        continue;
                    }
                }

                // Is a pickmin (LethalMin mod) holding the object ?
                if (PluginRuntimeProvider.Context.IsModLethalMinLoaded)
                {
                    if (LethalMinHook.IsGrabbableObjectHeldByPikminMod?.Invoke(grabbableObject) ?? false)
                    {
                        continue;
                    }
                }

                // Grabbable object ?
                if (!IsGrabbableObjectGrabbable(grabbableObject))
                {
                    continue;
                }

                // Object close to awareness distance ?
                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_AWARNESS * Const.INTERN_OBJECT_AWARNESS)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"awareness {grabbableObject.name}");
                }
                // Object visible ?
                else if (!Physics.Linecast(eye.position, gameObjectPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = gameObjectPosition - eye.position;
                    if (Vector3.Angle(eye.forward, to) < Const.INTERN_FOV)
                    {
                        // Object in FOV
                        PluginLoggerHook.LogDebug?.Invoke($"LOS {grabbableObject.name}");
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    // Object not in line of sight
                    continue;
                }

                return grabbableObject;
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
            if (grabbableObject == null
                || !grabbableObject.gameObject.activeSelf)
            {
                return false;
            }

            if (grabbableObject.isHeld
                || !grabbableObject.grabbable
                || grabbableObject.deactivated)
            {
                return false;
            }

            RagdollGrabbableObject? ragdollGrabbableObject = grabbableObject as RagdollGrabbableObject;
            if (ragdollGrabbableObject != null)
            {
                if (!ragdollGrabbableObject.grabbableToEnemies)
                {
                    return false;
                }
            }

            // Item just dropped, should wait a bit before grab it again
            if (DictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return false;
                }
            }

            // Is item too close to entrance (with config option enabled)
            if (!PluginRuntimeProvider.Context.Config.GrabItemsNearEntrances)
            {
                for (int j = 0; j < EntrancesTeleportArray.Length; j++)
                {
                    if ((grabbableObject.transform.position - EntrancesTeleportArray[j].entrancePoint.position).sqrMagnitude < Const.DISTANCE_ITEMS_TO_ENTRANCE * Const.DISTANCE_ITEMS_TO_ENTRANCE)
                    {
                        return false;
                    }
                }
            }

            // Trim dictionnary if too large
            TrimDictJustDroppedItems();

            // Is the item reachable with the agent pathfind ? (only owner knows and calculate) real position of ai intern)
            if (IsOwner
                && PathIsIntersectedByLineOfSight(grabbableObject.transform.position, false, false))
            {
                //PluginLoggerHook.LogDebug?.Invoke($"object {grabbableObject.name} pathfind is not reachable");
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
                PluginLoggerHook.LogDebug?.Invoke($"TrimDictJustDroppedItems Count{DictJustDroppedItems.Count}");
                var itemsToClean = DictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS);
                foreach (var item in itemsToClean)
                {
                    DictJustDroppedItems.Remove(item.Key);
                }
            }
        }

        public void SetInternInElevator()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;

            if (NpcController == null)
            {
                return;
            }

            if (RagdollInternBody != null
                && RagdollInternBody.IsRagdollBodyHeld())
            {
                return;
            }

            bool wasInHangarShipRoom = NpcController.Npc.isInHangarShipRoom;
            if (!NpcController.Npc.isInElevator
                && instanceSOR.shipBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInElevator = true;
            }

            if (NpcController.Npc.isInElevator
                && !wasInHangarShipRoom
                && instanceSOR.shipInnerRoomBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInHangarShipRoom = true;
            }
            else if (NpcController.Npc.isInElevator
                && !instanceSOR.shipBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInElevator = false;
                NpcController.Npc.isInHangarShipRoom = false;
                wasInHangarShipRoom = false;

                if (!AreHandsFree())
                {
                    NpcController.Npc.SetItemInElevator(droppedInShipRoom: false, droppedInElevator: false, HeldItem);
                }
            }

            if (wasInHangarShipRoom != NpcController.Npc.isInHangarShipRoom
                && !NpcController.Npc.isInHangarShipRoom
                && !AreHandsFree())
            {
                NpcController.Npc.SetItemInElevator(droppedInShipRoom: false, droppedInElevator: true, HeldItem);
            }
        }

        private bool IsGrabbableObjectBlackListed(GameObject gameObjectToEvaluate)
        {
            // Bee nest
            if (!PluginRuntimeProvider.Context.Config.GrabBeesNest
                && gameObjectToEvaluate.name.Contains("RedLocustHive"))
            {
                return true;
            }

            // Dead bodies
            if (!PluginRuntimeProvider.Context.Config.GrabDeadBodies
                && gameObjectToEvaluate.name.Contains("RagdollGrabbableObject")
                && gameObjectToEvaluate.tag == "PhysicsProp"
                && gameObjectToEvaluate.GetComponentInParent<DeadBodyInfo>() != null)
            {
                return true;
            }

            // Maneater
            if (!PluginRuntimeProvider.Context.Config.GrabManeaterBaby
                && gameObjectToEvaluate.name.Contains("CaveDwellerEnemy"))
            {
                return true;
            }

            // Wheelbarrow
            if (!PluginRuntimeProvider.Context.Config.GrabWheelbarrow
                && gameObjectToEvaluate.name.Contains("Wheelbarrow"))
            {
                return true;
            }

            // ShoppingCart
            if (!PluginRuntimeProvider.Context.Config.GrabShoppingCart
                && gameObjectToEvaluate.name.Contains("ShoppingCart"))
            {
                return true;
            }

            return false;
        }

        private void InitImportantColliders()
        {
            if (dictComponentByCollider == null)
            {
                dictComponentByCollider = new Dictionary<string, Component>();
            }
            else
            {
                dictComponentByCollider.Clear();
            }

            BridgeTrigger[] bridgeTriggers = FindObjectsOfType<BridgeTrigger>(includeInactive: false);
            for (int i = 0; i < bridgeTriggers.Length; i++)
            {
                Component[] bridgePhysicsPartsContainerComponents = bridgeTriggers[i].bridgePhysicsPartsContainer.gameObject.GetComponentsInChildren<Transform>();
                for (int j = 0; j < bridgePhysicsPartsContainerComponents.Length; j++)
                {
                    if (bridgePhysicsPartsContainerComponents[j].name == "Mesh")
                    {
                        continue;
                    }

                    if (!dictComponentByCollider.ContainsKey(bridgePhysicsPartsContainerComponents[j].name))
                    {
                        dictComponentByCollider.Add(bridgePhysicsPartsContainerComponents[j].name, bridgeTriggers[i]);
                    }
                }
            }

            //foreach (var a in dictComponentByCollider)
            //{
            //    PluginLoggerHook.LogDebug?.Invoke($"dictComponentByCollider {a.Key} {a.Value}");
            //    ComponentUtil.ListAllComponents(((BridgeTrigger)a.Value).bridgePhysicsPartsContainer.gameObject);
            //}
        }

        public void HideShowLevelStickerBetaBadge(bool show)
        {
            MeshRenderer[] componentsInChildren = NpcController.Npc.gameObject.GetComponentsInChildren<MeshRenderer>();
            (from x in componentsInChildren
             where x.gameObject.name == "LevelSticker"
             select x).First().enabled = show;
            (from x in componentsInChildren
             where x.gameObject.name == "BetaBadge"
             select x).First().enabled = show;
        }

        public void SetInternLookAt(Vector3? position = null)
        {
            if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
            {
                LookAtWhatPlayerPointingAt();
            }
            else
            {
                if (position.HasValue)
                {
                    NpcController.OrderToLookAtPlayer(position.Value + new Vector3(0, 2.35f, 0));
                }
                else
                {
                    // Looking at player or forward
                    PlayerControllerB? playerToLook = CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    if (playerToLook != null)
                    {
                        NpcController.OrderToLookAtPlayer(playerToLook.playerEye.position);
                    }
                    else
                    {
                        NpcController.OrderToLookForward();
                    }
                }
            }
        }

        public void LookAtWhatPlayerPointingAt()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            // Look where the target player is looking
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                NpcController.SetTurnBodyTowardsDirection(localPlayer.gameplayCamera.transform.forward);
                NpcController.OrderToLookForward();
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
                        NpcController.OrderToLookAtPosition(hit.point);
                        NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        NpcController.SetTurnBodyTowardsDirection(localPlayer.gameplayCamera.transform.forward);
                        NpcController.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    NpcController.OrderToLookAtPosition(hit.point);
                    NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }

        #region Voices

        public void UpdateInternVoiceEffects()
        {
            PlayerControllerB internController = NpcController.Npc;
            int internPlayerClientID = (int)internController.playerClientId;
            PlayerControllerB spectatedPlayerScript;
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
            {
                spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
            }
            else
            {
                spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController;
            }

            bool walkieTalkie = internController.speakingToWalkieTalkie
                                && spectatedPlayerScript.holdingWalkieTalkie
                                && internController != spectatedPlayerScript;

            AudioLowPassFilter audioLowPassFilter = NpcController.AudioLowPassFilterComponent;
            OccludeAudio occludeAudio = NpcController.OccludeAudioComponent;
            audioLowPassFilter.enabled = true;
            occludeAudio.overridingLowPass = walkieTalkie || internController.voiceMuffledByEnemy;
            NpcController.AudioHighPassFilterComponent.enabled = walkieTalkie;
            if (!walkieTalkie)
            {
                creatureVoice.spatialBlend = 1f;
                creatureVoice.bypassListenerEffects = false;
                creatureVoice.bypassEffects = false;
                creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                audioLowPassFilter.lowpassResonanceQ = 1f;
            }
            else
            {
                creatureVoice.spatialBlend = 0f;
                if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                {
                    creatureVoice.panStereo = 0f;
                    creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                    creatureVoice.bypassListenerEffects = false;
                    creatureVoice.bypassEffects = false;
                }
                else
                {
                    creatureVoice.panStereo = 0.4f;
                    creatureVoice.bypassListenerEffects = false;
                    creatureVoice.bypassEffects = false;
                    creatureVoice.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[internPlayerClientID];
                }
                occludeAudio.lowPassOverride = 4000f;
                audioLowPassFilter.lowpassResonanceQ = 3f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayAudioServerRpc(string smallPathAudioClip, int enumTalkativeness)
        {
            PlayAudioClientRpc(smallPathAudioClip, enumTalkativeness);
        }

        [ClientRpc]
        private void PlayAudioClientRpc(string smallPathAudioClip, int enumTalkativeness)
        {
            if (enumTalkativeness == PluginRuntimeProvider.Context.Config.Talkativeness
                || InternIdentity.Voice.CanPlayAudioAfterCooldown())
            {
                AudioManager.Instance.PlayAudio(smallPathAudioClip, InternIdentity.Voice);
            }
        }

        #endregion

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
        public void TeleportIntern(Vector3 pos, bool? setOutside = null, bool isUsingEntrance = false)
        {
            // teleport body
            TeleportAgentAIAndBody(pos);

            // Set AI outside or inside dungeon
            if (!setOutside.HasValue)
            {
                setOutside = pos.y >= -80f;
            }

            NpcController.Npc.isInsideFactory = !setOutside.Value;
            if (isOutside != setOutside.Value)
            {
                SetEnemyOutside(setOutside.Value);
            }

            // Using main entrance or fire exits ?
            if (isUsingEntrance)
            {
                NpcController.Npc.thisPlayerBody.RotateAround(NpcController.Npc.thisPlayerBody.transform.position, Vector3.up, 180f);
                TimeSinceTeleporting = Time.timeSinceLevelLoad;
                EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside.Value);
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
        private void TeleportAgentAIAndBody(Vector3 pos)
        {
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 2.7f);
            serverPosition = navMeshPosition;
            NpcController.Npc.transform.position = navMeshPosition;

            if (agent == null
                || !agent.enabled)
            {
                transform.position = navMeshPosition;
            }
            else
            {
                agent.enabled = false;
                transform.position = navMeshPosition;
                agent.enabled = true;
            }

            // For CullFactory mod
            if (HeldItem != null)
            {
                HeldItem.EnableItemMeshes(true);
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
            if (enteringVehicle)
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }
                NpcController.Npc.transform.position = pos;
                StateControllerMovement = EnumStateControllerMovement.Fixed;
            }
            else
            {
                TeleportIntern(pos);
                StateControllerMovement = EnumStateControllerMovement.FollowAgent;
            }

            NpcController.IsControllerInCruiser = enteringVehicle;

            if (NpcController.IsControllerInCruiser)
            {
                if (networkBehaviourReferenceVehicle.TryGet(out VehicleController vehicleController))
                {
                    // Attach intern to vehicle
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} enters vehicle");
                    ReParentIntern(vehicleController.transform);
                }

                StopSinkingState();
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} exits vehicle");
                ReParentIntern(NpcController.Npc.playersManager.playersContainer);
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
            if (OwnerClientId != newTarget.actualClientId)
            {
                // Changes the ownership of the intern, on server and client directly
                ChangeOwnershipOfEnemy(newTarget.actualClientId);

                if (IsServer)
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
            PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts[playerid];
            SetMovingTowardsTargetPlayer(targetPlayer);

            SetDestinationToPositionInternAI(this.targetPlayer.transform.position);

            SetCommandToFollowPlayer();
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
        [ServerRpc(RequireOwnership = false)]
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

            bool flag = NpcController.Npc.currentFootstepSurfaceIndex == 8 && (IsOwner && NpcController.IsTouchingGround || isPlayerGrounded);
            if (NpcController.Npc.bleedingHeavily || flag)
            {
                NpcController.Npc.DropBlood(Vector3.down, NpcController.Npc.bleedingHeavily, flag);
            }
            NpcController.Npc.timeSincePlayerMoving = 0f;

            if (IsOwner)
            {
                // Only update if not owner
                return;
            }

            NpcController.Npc.isExhausted = exhausted;
            NpcController.Npc.isInElevator = inElevator;
            NpcController.Npc.isInHangarShipRoom = isInShip;

            if (!AreHandsFree()
                && HeldItem != null
                && HeldItem.isInShipRoom != isInShip)
            {
                HeldItem.isInElevator = inElevator;
                NpcController.Npc.SetItemInElevator(droppedInShipRoom: isInShip, droppedInElevator: inElevator, HeldItem);
            }

            NpcController.Npc.oldPlayerPosition = NpcController.Npc.serverPlayerPosition;
            if (!NpcController.Npc.inVehicleAnimation)
            {
                NpcController.Npc.serverPlayerPosition = newPos;
            }
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
        public void SyncUpdateInternRotationAndLook(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            if (IsServer)
            {
                UpdateInternRotationAndLookClientRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
            }
            else
            {
                UpdateInternRotationAndLookServerRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
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
        private void UpdateInternRotationAndLookServerRpc(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
        {
            UpdateInternRotationAndLookClientRpc(stateIndicator, direction, intEnumObjectsLookingAt, playerEyeToLookAt, positionToLookAt);
        }

        /// <summary>
        /// Client side, update the intern body rotation and rotation of head (where he looks).
        /// </summary>
        /// <param name="direction">Direction to turn body towards to</param>
        /// <param name="intEnumObjectsLookingAt">State to know where the intern should look</param>
        /// <param name="playerEyeToLookAt">Position of the player eyes to look at</param>
        /// <param name="positionToLookAt">Position to look at</param>
        [ClientRpc]
        private void UpdateInternRotationAndLookClientRpc(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt)
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

            // Update state indicator
            // Actually, too much cluter, indicator just for owner for now
            //this.stateIndicatorServer = stateIndicator;

            // Update direction
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
        [ServerRpc(RequireOwnership = false)]
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

            NpcController.ApplyUpdateInternAnimationsNotOwner(animationState, animationSpeed);
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

            UpdateInternSpecialAnimationServerRpc(specialAnimation, timed, climbingLadder);
        }

        /// <summary>
        /// Server side, call clients to update the intern special animation
        /// </summary>
        /// <param name="specialAnimation">Is in special animation ?</param>
        /// <param name="timed">Wait time of the special animation to end</param>
        /// <param name="climbingLadder">Is climbing ladder ?</param>
        [ServerRpc(RequireOwnership = false)]
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

            PlayerControllerBHook.IsInSpecialAnimationClientRpc_ReversePatch?.Invoke(NpcController.Npc, specialAnimation, timed, climbingLadder);
            NpcController.Npc.ResetZAndXRotation();
        }

        #endregion

        #region SyncDeadBodyPosition RPC

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
            PlayerControllerBHook.SyncBodyPositionClientRpc_ReversePatch?.Invoke(NpcController.Npc, newBodyPosition);
        }

        #endregion

        #region SyncFaceUnderwater

        [ServerRpc(RequireOwnership = false)]
        public void SyncSetFaceUnderwaterServerRpc(bool isUnderwater)
        {
            SyncSetFaceUnderwaterClientRpc(isUnderwater);
        }

        [ClientRpc]
        private void SyncSetFaceUnderwaterClientRpc(bool isUnderwater)
        {
            NpcController.Npc.isUnderwater = isUnderwater;
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
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (!itemGiven)
            {
                if (!IsGrabbableObjectGrabbable(grabbableObject))
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} grabbableObject {grabbableObject} not grabbable");
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
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (HeldItem == grabbableObject)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} cannot grab already held item {grabbableObject} on client #{NetworkManager.LocalClientId}");
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
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} try to grab item {grabbableObject} on client #{NetworkManager.LocalClientId}");
            HeldItem = grabbableObject;

            grabbableObject.GrabItemFromEnemy(this);
            grabbableObject.parentObject = NpcController.Npc.serverItemHolder;
            grabbableObject.playerHeldBy = NpcController.Npc;
            grabbableObject.isHeld = true;
            grabbableObject.hasHitGround = false;
            grabbableObject.isInFactory = NpcController.Npc.isInsideFactory;
            grabbableObject.EquipItem();

            NpcController.Npc.isHoldingObject = true;
            NpcController.Npc.currentlyHeldObjectServer = grabbableObject;
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
            SetSpecialGrabAnimationBool(true, grabbableObject);

            if (grabObjectCoroutine != null)
            {
                StopCoroutine(grabObjectCoroutine);
            }
            grabObjectCoroutine = StartCoroutine(GrabAnimationCoroutine());

            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Grabbed item {grabbableObject} on client #{NetworkManager.LocalClientId}");
        }

        /// <summary>
        /// Coroutine for the grab animation
        /// </summary>
        /// <returns></returns>
        private IEnumerator GrabAnimationCoroutine()
        {
            if (HeldItem != null)
            {
                float grabAnimationTime = HeldItem.itemProperties.grabAnimationTime > 0f ? HeldItem.itemProperties.grabAnimationTime : 0.4f;
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
                    PluginLoggerHook.LogError?.Invoke("An item tried to set an animator bool which does not exist: " + item.itemProperties.grabAnim);
                }
            }
        }

        #endregion

        #region Drop item RPC

        /// <summary>
        /// Make the intern drop his item like an enemy, but update the body (<c>PlayerControllerB</c>) too.
        /// </summary>
        public void DropItem()
        {
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Try to drop item on client #{NetworkManager.LocalClientId}");
            if (HeldItem == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} Try to drop not held item on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabbableObject grabbableObject = HeldItem;
            bool placeObject = false;
            Vector3 placePosition = default;
            NetworkObject parentObjectTo = null!;
            bool matchRotationOfParent = true;
            Vector3 vector;
            NetworkObject physicsRegionOfDroppedObject = grabbableObject.GetPhysicsRegionOfDroppedObject(NpcController.Npc, out vector);
            if (physicsRegionOfDroppedObject != null)
            {
                placePosition = vector;
                parentObjectTo = physicsRegionOfDroppedObject;
                placeObject = true;
                matchRotationOfParent = false;
            }

            if (placeObject)
            {
                if (parentObjectTo == null)
                {
                    if (NpcController.Npc.isInElevator)
                    {
                        placePosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placePosition);
                    }
                    else
                    {
                        placePosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(placePosition);
                    }
                    int floorYRot2 = (int)transform.localEulerAngles.y;

                    // on client
                    SetObjectAsNoLongerHeld(grabbableObject,
                                            NpcController.Npc.isInElevator,
                                            NpcController.Npc.isInHangarShipRoom,
                                            placePosition,
                                            floorYRot2);
                    // for other clients
                    SetObjectAsNoLongerHeldServerRpc(new DropItemNetworkSerializable()
                    {
                        DroppedInElevator = NpcController.Npc.isInElevator,
                        DroppedInShipRoom = NpcController.Npc.isInHangarShipRoom,
                        FloorYRot = floorYRot2,
                        GrabbedObject = grabbableObject.NetworkObject,
                        TargetFloorPosition = placePosition
                    });
                }
                else
                {
                    // on client
                    PlaceGrabbableObject(grabbableObject, parentObjectTo.transform, placePosition, matchRotationOfParent);

                    // for other clients
                    PlaceGrabbableObjectServerRpc(new PlaceItemNetworkSerializable()
                    {
                        GrabbedObject = grabbableObject.NetworkObject,
                        MatchRotationOfParent = matchRotationOfParent,
                        ParentObject = parentObjectTo,
                        PlacePositionOffset = placePosition
                    });
                }
            }
            else
            {
                bool droppedInElevator = NpcController.Npc.isInElevator;
                Vector3 targetFloorPosition;
                if (!NpcController.Npc.isInElevator)
                {
                    Vector3 vector2;
                    if (grabbableObject.itemProperties.allowDroppingAheadOfPlayer)
                    {
                        vector2 = DropItemAheadOfPlayer(grabbableObject, NpcController.Npc);
                    }
                    else
                    {
                        vector2 = grabbableObject.GetItemFloorPosition(default);
                    }
                    if (!NpcController.Npc.playersManager.shipBounds.bounds.Contains(vector2))
                    {
                        targetFloorPosition = NpcController.Npc.playersManager.propsContainer.InverseTransformPoint(vector2);
                    }
                    else
                    {
                        droppedInElevator = true;
                        targetFloorPosition = NpcController.Npc.playersManager.elevatorTransform.InverseTransformPoint(vector2);
                    }
                }
                else
                {
                    Vector3 vector2 = grabbableObject.GetItemFloorPosition(default);
                    if (!NpcController.Npc.playersManager.shipBounds.bounds.Contains(vector2))
                    {
                        droppedInElevator = false;
                        targetFloorPosition = NpcController.Npc.playersManager.propsContainer.InverseTransformPoint(vector2);
                    }
                    else
                    {
                        targetFloorPosition = NpcController.Npc.playersManager.elevatorTransform.InverseTransformPoint(vector2);
                    }
                }
                int floorYRot = (int)transform.localEulerAngles.y;

                // on client
                SetObjectAsNoLongerHeld(grabbableObject,
                                        droppedInElevator,
                                        NpcController.Npc.isInHangarShipRoom,
                                        targetFloorPosition,
                                        floorYRot);

                // for other clients
                SetObjectAsNoLongerHeldServerRpc(new DropItemNetworkSerializable()
                {
                    DroppedInElevator = droppedInElevator,
                    DroppedInShipRoom = NpcController.Npc.isInHangarShipRoom,
                    FloorYRot = floorYRot,
                    GrabbedObject = grabbableObject.NetworkObject,
                    TargetFloorPosition = targetFloorPosition
                });
            }


        }

        private Vector3 DropItemAheadOfPlayer(GrabbableObject grabbableObject, PlayerControllerB player)
        {
            Vector3 vector;
            Ray ray = new Ray(transform.position + Vector3.up * 0.4f, player.gameplayCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1.7f, 268438273, QueryTriggerInteraction.Ignore))
            {
                vector = ray.GetPoint(Mathf.Clamp(hit.distance - 0.3f, 0.01f, 2f));
            }
            else
            {
                vector = ray.GetPoint(1.7f);
            }
            Vector3 itemFloorPosition = grabbableObject.GetItemFloorPosition(vector);
            if (itemFloorPosition == vector)
            {
                itemFloorPosition = grabbableObject.GetItemFloorPosition(default);
            }
            return itemFloorPosition;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetObjectAsNoLongerHeldServerRpc(DropItemNetworkSerializable dropItemNetworkSerializable)
        {
            NetworkObject networkObject;
            if (dropItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null))
            {
                SetObjectAsNoLongerHeldClientRpc(dropItemNetworkSerializable);
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"Intern {NpcController.Npc.playerUsername} on client #{NetworkManager.LocalClientId} (server) drop item : Object was not thrown because it does not exist on the server.");
            }
        }

        [ClientRpc]
        private void SetObjectAsNoLongerHeldClientRpc(DropItemNetworkSerializable dropItemNetworkSerializable)
        {
            if (HeldItem == null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} held item already dropped, on client #{NetworkManager.LocalClientId}");
                return;
            }

            NetworkObject networkObject;
            if (dropItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null))
            {
                SetObjectAsNoLongerHeld(networkObject.GetComponent<GrabbableObject>(),
                                        dropItemNetworkSerializable.DroppedInElevator,
                                        dropItemNetworkSerializable.DroppedInShipRoom,
                                        dropItemNetworkSerializable.TargetFloorPosition,
                                        dropItemNetworkSerializable.FloorYRot);
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"Intern {NpcController.Npc.playerUsername} on client #{NetworkManager.LocalClientId} drop item : The server did not have a reference to the held object");
            }
        }

        private void SetObjectAsNoLongerHeld(GrabbableObject grabbableObject,
                                             bool droppedInElevator,
                                             bool droppedInShipRoom,
                                             Vector3 targetFloorPosition,
                                             int floorYRot = -1)
        {
            grabbableObject.heldByPlayerOnServer = false;
            grabbableObject.parentObject = null;
            if (droppedInElevator)
            {
                grabbableObject.transform.SetParent(NpcController.Npc.playersManager.elevatorTransform, true);
            }
            else
            {
                grabbableObject.transform.SetParent(NpcController.Npc.playersManager.propsContainer, true);
            }

            NpcController.Npc.SetItemInElevator(droppedInShipRoom, droppedInElevator, grabbableObject);
            grabbableObject.EnablePhysics(true);
            grabbableObject.EnableItemMeshes(true);
            grabbableObject.isHeld = false;
            grabbableObject.isPocketed = false;
            grabbableObject.fallTime = 0f;
            grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
            grabbableObject.targetFloorPosition = targetFloorPosition;
            grabbableObject.floorYRot = floorYRot;

            EndDropItem(grabbableObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaceGrabbableObjectServerRpc(PlaceItemNetworkSerializable placeItemNetworkSerializable)
        {
            NetworkObject networkObject;
            NetworkObject networkObject2;
            if (placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null)
                && placeItemNetworkSerializable.ParentObject.TryGet(out networkObject2, null))
            {
                PlaceGrabbableObjectClientRpc(placeItemNetworkSerializable);
                return;
            }

            NetworkObject networkObject3;
            if (!placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject3, null))
            {
                PluginLoggerHook.LogError?.Invoke($"Object placement not synced to clients, missing reference to a network object: placing object with id: {placeItemNetworkSerializable.GrabbedObject.NetworkObjectId}; intern {NpcController.Npc.playerUsername}");
                return;
            }
            NetworkObject networkObject4;
            if (!placeItemNetworkSerializable.ParentObject.TryGet(out networkObject4, null))
            {
                PluginLoggerHook.LogError?.Invoke($"Object placement not synced to clients, missing reference to a network object: parent object with id: {placeItemNetworkSerializable.ParentObject.NetworkObjectId}; intern {NpcController.Npc.playerUsername}");
            }
        }

        [ClientRpc]
        private void PlaceGrabbableObjectClientRpc(PlaceItemNetworkSerializable placeItemNetworkSerializable)
        {
            NetworkObject networkObject;
            if (placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null))
            {
                GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
                NetworkObject networkObject2;
                if (placeItemNetworkSerializable.ParentObject.TryGet(out networkObject2, null))
                {
                    PlaceGrabbableObject(grabbableObject,
                                              networkObject2.transform,
                                              placeItemNetworkSerializable.PlacePositionOffset,
                                              placeItemNetworkSerializable.MatchRotationOfParent);
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"Reference to parent object when placing was missing. object: {grabbableObject} placed by intern #{NpcController.Npc.playerUsername}");
                }
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke("The server did not have a reference to the held object (when attempting to PLACE object on client.)");
            }
        }

        private void PlaceGrabbableObject(GrabbableObject placeObject, Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent)
        {
            if (HeldItem == null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} held item already placed, on client #{NetworkManager.LocalClientId}");
                return;
            }

            PlayerPhysicsRegion componentInChildren = parentObject.GetComponentInChildren<PlayerPhysicsRegion>();
            if (componentInChildren != null && componentInChildren.allowDroppingItems)
            {
                parentObject = componentInChildren.physicsTransform;
            }
            placeObject.EnablePhysics(true);
            placeObject.EnableItemMeshes(true);
            placeObject.isHeld = false;
            placeObject.isPocketed = false;
            placeObject.heldByPlayerOnServer = false;
            NpcController.Npc.SetItemInElevator(NpcController.Npc.isInHangarShipRoom, NpcController.Npc.isInElevator, placeObject);
            placeObject.parentObject = null;
            placeObject.transform.SetParent(parentObject, true);
            placeObject.startFallingPosition = placeObject.transform.localPosition;
            placeObject.transform.localScale = placeObject.originalScale;
            placeObject.transform.localPosition = positionOffset;
            placeObject.targetFloorPosition = positionOffset;
            if (!matchRotationOfParent)
            {
                placeObject.fallTime = 0f;
            }
            else
            {
                placeObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                placeObject.fallTime = 1.1f;
            }
            placeObject.OnPlaceObject();

            EndDropItem(placeObject);
        }

        private void EndDropItem(GrabbableObject grabbableObject)
        {
            grabbableObject.DiscardItem();
            SetSpecialGrabAnimationBool(false, grabbableObject);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, true);
            NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);

            DictJustDroppedItems[grabbableObject] = Time.realtimeSinceStartup;
            HeldItem = null;
            NpcController.Npc.isHoldingObject = false;
            NpcController.Npc.currentlyHeldObjectServer = null;
            NpcController.Npc.twoHanded = false;
            NpcController.Npc.twoHandedAnimation = false;
            NpcController.GrabbedObjectValidated = false;

            float weightToLose = grabbableObject.itemProperties.weight - 1f < 0f ? 0f : grabbableObject.itemProperties.weight - 1f;
            NpcController.Npc.carryWeight = Mathf.Clamp(NpcController.Npc.carryWeight - weightToLose, 1f, 10f);

            SyncBatteryIntern(grabbableObject, (int)(grabbableObject.insertedBattery.charge * 100f));
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} dropped {grabbableObject}, on client #{NetworkManager.LocalClientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncBatteryInternServerRpc(NetworkObjectReference networkObjectReferenceGrabbableObject, int charge)
        {
            SyncBatteryInternClientRpc(networkObjectReferenceGrabbableObject, charge);
        }

        [ClientRpc]
        private void SyncBatteryInternClientRpc(NetworkObjectReference networkObjectReferenceGrabbableObject, int charge)
        {
            if (!networkObjectReferenceGrabbableObject.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"SyncBatteryInternClientRpc : Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncBatteryInternClientRpc : Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            SyncBatteryIntern(grabbableObject, charge);
        }

        private void SyncBatteryIntern(GrabbableObject grabbableObject, int charge)
        {
            float num = charge / 100f;
            grabbableObject.insertedBattery = new Battery(num <= 0f, num);
            grabbableObject.ChargeBatteries();
        }

        #endregion

        #region Give item to intern RPC

        [ServerRpc(RequireOwnership = false)]
        public void GiveItemToInternServerRpc(ulong playerClientIdGiver, NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternServerRpc for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternServerRpc for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            GiveItemToInternClientRpc(playerClientIdGiver, networkObjectReference);
        }

        [ClientRpc]
        private void GiveItemToInternClientRpc(ulong playerClientIdGiver, NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternClientRpc for InternAI {InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternClientRpc for InternAI {InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            GiveItemToIntern(playerClientIdGiver, grabbableObject);
        }

        private void GiveItemToIntern(ulong playerClientIdGiver, GrabbableObject grabbableObject)
        {
            PluginLoggerHook.LogDebug?.Invoke($"GiveItemToIntern playerClientIdGiver {playerClientIdGiver}, localPlayerController {StartOfRound.Instance.localPlayerController.playerClientId}");
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerClientIdGiver];

            // Discard for player
            if (player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                PlayerControllerBHook.SetSpecialGrabAnimationBool_ReversePatch?.Invoke(player, false, player.currentlyHeldObjectServer);
                player.playerBodyAnimator.SetBool("cancelHolding", true);
                player.playerBodyAnimator.SetTrigger("Throw");
                HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
                HUDManager.Instance.holdingTwoHandedItem.enabled = false;
                HUDManager.Instance.ClearControlTips();
            }

            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                if (player.ItemSlots[i] == grabbableObject)
                {
                    player.ItemSlots[i] = null;
                }
            }

            grabbableObject.EnablePhysics(true);
            grabbableObject.EnableItemMeshes(true);
            grabbableObject.parentObject = null;
            grabbableObject.heldByPlayerOnServer = false;
            grabbableObject.DiscardItem();

            player.isHoldingObject = false;
            player.currentlyHeldObjectServer = null;
            player.twoHanded = false;
            player.twoHandedAnimation = false;

            float weightToLose = grabbableObject.itemProperties.weight - 1f < 0f ? 0f : grabbableObject.itemProperties.weight - 1f;
            player.carryWeight = Mathf.Clamp(player.carryWeight - weightToLose, 1f, 10f);

            SyncBatteryInternServerRpc(grabbableObject.NetworkObject, (int)(grabbableObject.insertedBattery.charge * 100f));

            // Intern grab item
            GrabItem(grabbableObject);
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
                CentipedeAI[] array = FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].clingingToPlayer == this)
                    {
                        return;
                    }
                }
                DamageIntern(damageAmount, CauseOfDeath.Bludgeoning, 0, false, default);
            }

            NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
            if (NpcController.Npc.health < MaxHealthPercent(6))
            {
                NpcController.Npc.DropBlood(hitDirection, true, false);
                NpcController.Npc.bodyBloodDecals[0].SetActive(true);
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].AddBloodToBody();
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].movementAudio.PlayOneShot(StartOfRound.Instance.bloodGoreSFX);
            }
        }

        #endregion

        #region Damage intern RPC

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSFX = false, int hitID = -1)
        {
            // The HitEnemy function works with player controller instead
            return;
        }

        /// <summary>
        /// Sync the damage taken by the intern between server and clients
        /// </summary>
        /// <remarks>
        /// Better to call <see cref="PlayerControllerB.DamagePlayer"><c>PlayerControllerB.DamagePlayer</c></see> so prefixes from other mods can activate. (ex : peepers)
        /// The base game function will be ignored because the intern playerController is not owned because not spawned
        /// </remarks>
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
            PluginLoggerHook.LogDebug?.Invoke($"SyncDamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (IsServer)
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
            PluginLoggerHook.LogDebug?.Invoke(@$"DamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername},
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
                && !NpcController.Npc.criticallyInjured
                && damageNumber < MaxHealthPercent(50)
                && MaxHealthPercent(10) != MaxHealthPercent(20))
            {
                NpcController.Npc.health = 1;
            }
            else
            {
                NpcController.Npc.health = Mathf.Clamp(NpcController.Npc.health - damageNumber, 0, MaxHealth);
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
                KillIntern(force, spawnBody: true, causeOfDeath, deathAnimation, positionOffset: default);
            }
            else
            {
                // Critically injured
                if ((NpcController.Npc.health < MaxHealthPercent(10) || NpcController.Npc.health == 1)
                    && !NpcController.Npc.criticallyInjured)
                {
                    // Client side only, since we are already in an rpc send to all clients
                    MakeCriticallyInjured();
                }
                else
                {
                    // Limit sprinting when close to death
                    if (damageNumber >= MaxHealthPercent(10))
                    {
                        NpcController.Npc.sprintMeter = Mathf.Clamp(NpcController.Npc.sprintMeter + damageNumber / 125f, 0f, 1f);
                    }
                }
                if (fallDamage)
                {
                    NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
                }
                else
                {
                    NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
                }

                // Audio, already in client rpc method so no sync necessary
                InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                {
                    VoiceState = EnumVoicesState.Hit,
                    CanTalkIfOtherInternTalk = true,
                    WaitForCooldown = false,
                    CutCurrentVoiceStateToTalk = true,
                    CanRepeatVoiceState = true,

                    ShouldSync = false,
                    IsInternInside = NpcController.Npc.isInsideFactory,
                    AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
                });
            }

            NpcController.Npc.takingFallDamage = false;
            if (!NpcController.Npc.inSpecialInteractAnimation)
            {
                NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_DAMAGE);
            }
            NpcController.Npc.specialAnimationWeight = 1f;
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);
        }

        public void HealthRegen()
        {
            if (NpcController.Npc.health < MaxHealthPercent(20)
                || NpcController.Npc.health == 1)
            {
                if (NpcController.Npc.healthRegenerateTimer <= 0f)
                {
                    NpcController.Npc.healthRegenerateTimer = healthRegenerateTimerMax;
                    NpcController.Npc.health = NpcController.Npc.health + 1 > MaxHealth ? MaxHealth : NpcController.Npc.health + 1;
                    if (NpcController.Npc.criticallyInjured &&
                        (NpcController.Npc.health >= MaxHealthPercent(20) || MaxHealth == 1))
                    {
                        Heal();
                    }
                }
                else
                {
                    NpcController.Npc.healthRegenerateTimer -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Update the state of critically injured
        /// </summary>
        private void MakeCriticallyInjured()
        {
            NpcController.Npc.bleedingHeavily = true;
            NpcController.Npc.criticallyInjured = true;
            NpcController.Npc.hasBeenCriticallyInjured = true;
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, true);
        }

        /// <summary>
        /// Heal the intern
        /// </summary>
        private void Heal()
        {
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.criticallyInjured = false;
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, false);
        }

        #endregion

        #region Kill intern RPC

        public override void KillEnemy(bool destroy = false)
        {
            // The kill function works with player controller instead
            return;
        }

        /// <summary>
        /// Sync the action to kill intern between server and clients
        /// </summary>
        /// <remarks>
        /// Better to call <see cref="PlayerControllerB.KillPlayer"><c>PlayerControllerB.KillPlayer</c></see> so prefixes from other mods can activate. (ex : peepers)
        /// The base game function will be ignored because the intern playerController is not owned because not spawned
        /// </remarks>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody">Should a body be spawned ?</param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        public void SyncKillIntern(Vector3 bodyVelocity,
                                   bool spawnBody = true,
                                   CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                   int deathAnimation = 0,
                                   Vector3 positionOffset = default)
        {
            PluginLoggerHook.LogDebug?.Invoke($"SyncKillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (IsServer)
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
                GameObject gameObject = Instantiate(StartOfRound.Instance.ragdollGrabbableObjectPrefab, NpcController.Npc.playersManager.propsContainer);
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
            PluginLoggerHook.LogDebug?.Invoke(@$"KillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}
                            bodyVelocity {bodyVelocity}, spawnBody {spawnBody}, causeOfDeath {causeOfDeath}, deathAnimation {deathAnimation}, positionOffset {positionOffset}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            // If ragdoll body of intern is held
            // Release the intern before killing him
            if (RagdollInternBody.IsRagdollBodyHeld())
            {
                PlayerControllerB playerHolder = RagdollInternBody.GetPlayerHolder();
                ReleaseIntern(playerHolder.playerClientId);
                TeleportIntern(playerHolder.transform.position, !playerHolder.isInsideFactory, isUsingEntrance: false);
            }

            // Reset body
            NpcController.Npc.isPlayerDead = true;
            NpcController.Npc.isPlayerControlled = false;
            NpcController.Npc.thisPlayerModelArms.enabled = false;
            NpcController.Npc.localVisor.position = NpcController.Npc.playersManager.notSpawnedPosition.position;
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
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
            StopSinkingState();
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
                ResizeRagdoll(NpcController.Npc.deadBody.transform);
                // Replace body position or else disappear with shotgun or knife (don't know why)
                NpcController.Npc.deadBody.transform.position = NpcController.Npc.transform.position + Vector3.up + positionOffset;
                // Need to be set to true (don't know why) (so many mysteries unsolved tonight)
                NpcController.Npc.deadBody.canBeGrabbedBackByPlayers = true;
                InternIdentity.DeadBody = NpcController.Npc.deadBody;

                // Register body for animation culling
                InternManager.Instance.RegisterInternBodyForAnimationCulling(NpcController.Npc.deadBody, HasInternModelReplacementAPI());
            }
            NpcController.Npc.physicsParent = null;
            NpcController.Npc.overridePhysicsParent = null;
            NpcController.Npc.lastSyncedPhysicsParent = null;
            NpcController.CurrentInternPhysicsRegions.Clear();
            ReParentIntern(NpcController.Npc.playersManager.playersContainer);
            if (HeldItem != null)
            {
                DropItem();
            }
            NpcController.Npc.DisableJetpackControlsLocally();
            NpcController.IsControllerInCruiser = false;
            isEnemyDead = true;
            InternIdentity.Hp = 0;
            if (agent != null)
            {
                agent.enabled = false;
            }
            InternIdentity.Voice.StopAudioFadeOut();
            PluginLoggerHook.LogDebug?.Invoke($"Ran kill intern function for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            // Compat with revive company mod
            if (PluginRuntimeProvider.Context.IsModReviveCompanyLoaded)
            {
                ReviveCompanyHook.ReviveCompanySetPlayerDiedAt?.Invoke((int)Npc.playerClientId);
            }
        }

        #endregion

        #region Grab intern

        [ServerRpc(RequireOwnership = false)]
        public void GrabInternServerRpc(ulong idPlayerGrabberController)
        {
            GrabInternClientRpc(idPlayerGrabberController);
        }

        [ClientRpc]
        private void GrabInternClientRpc(ulong idPlayerGrabberController)
        {
            PlayerControllerB playerGrabberController = StartOfRound.Instance.allPlayerScripts[idPlayerGrabberController];

            InstantiateDeadBodyInfo(playerGrabberController);
            RagdollInternBody.SetGrabbedBy(playerGrabberController,
                                           ragdollBodyDeadBodyInfo,
                                           (int)idPlayerGrabberController);

            if (idPlayerGrabberController == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                // Add weight of body
                float weightToGain = RagdollInternBody.GetWeight() - 1f < 0f ? 0f : RagdollInternBody.GetWeight() - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight + weightToGain, 1f, 10f);

                weightToGain = NpcController.Npc.carryWeight - 1f < 0f ? 0f : NpcController.Npc.carryWeight - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight + weightToGain, 1f, 10f);

                // Register held interns
                InternManager.Instance.RegisterHeldInternForLocalPlayer((int)NpcController.Npc.playerClientId);
                // Hide of held ragdoll > 1 is done on BodyReplacementBasePatch after creation of replacementDeadBody
            }

            if (HeldItem != null)
            {
                HeldItem.EnableItemMeshes(enable: false);
            }

            // Hide intern
            NpcController.Npc.localVisor.position = NpcController.Npc.playersManager.notSpawnedPosition.position;
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
            NpcController.Npc.transform.position = NpcController.Npc.playersManager.notSpawnedPosition.position;

            StopSinkingState();
            NpcController.Npc.ResetFallGravity();
            NpcController.OrderToStopMoving();

            // Register body for animation culling
            InternManager.Instance.RegisterInternBodyForAnimationCulling(ragdollBodyDeadBodyInfo, HasInternModelReplacementAPI());
        }

        private void InstantiateDeadBodyInfo(PlayerControllerB playerReference, Vector3 bodyVelocity = default)
        {
            float num = 1.32f;
            int deathAnimation = 0;

            Transform parent = null!;
            if (playerReference.isInElevator)
            {
                parent = playerReference.playersManager.elevatorTransform;
            }

            Vector3 position = NpcController.Npc.thisPlayerBody.position + Vector3.up * num;
            Quaternion rotation = NpcController.Npc.thisPlayerBody.rotation;
            if (ragdollBodyDeadBodyInfo == null)
            {
                GameObject gameObject = Instantiate(NpcController.Npc.playersManager.playerRagdolls[deathAnimation],
                                                                       position,
                                                                       rotation,
                                                                       parent);
                ragdollBodyDeadBodyInfo = gameObject.GetComponent<DeadBodyInfo>();
            }

            ragdollBodyDeadBodyInfo.transform.position = position;
            ragdollBodyDeadBodyInfo.transform.rotation = rotation;
            ragdollBodyDeadBodyInfo.transform.parent = parent;

            if (playerReference.physicsParent != null)
            {
                ragdollBodyDeadBodyInfo.SetPhysicsParent(playerReference.physicsParent);
            }

            ragdollBodyDeadBodyInfo.parentedToShip = playerReference.isInElevator;
            ragdollBodyDeadBodyInfo.playerObjectId = (int)NpcController.Npc.playerClientId;

            Rigidbody[] componentsInChildren = ragdollBodyDeadBodyInfo.gameObject.GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].velocity = bodyVelocity;
            }

            // Scale ragdoll (without stretching the body parts)
            ResizeRagdoll(ragdollBodyDeadBodyInfo.transform);

            // False with model replacement API
            ragdollBodyDeadBodyInfo.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;

            // Set suit ID
            if (ragdollBodyDeadBodyInfo.setMaterialToPlayerSuit)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = ragdollBodyDeadBodyInfo.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.sharedMaterial = StartOfRound.Instance.unlockablesList.unlockables[NpcController.Npc.currentSuitID].suitMaterial;
                    skinnedMeshRenderer.renderingLayerMask = 513U | 1U << ragdollBodyDeadBodyInfo.playerObjectId + 12;
                }
            }
        }

        /// <summary>
        /// Scale ragdoll (without stretching the body parts)
        /// </summary>
        /// <param name="transform"></param>
        private void ResizeRagdoll(Transform transform)
        {
            // https://discussions.unity.com/t/joint-system-scale-problems/182154/4
            // https://stackoverflow.com/questions/68663372/how-to-enlarge-a-ragdoll-in-game-unity
            // Grab references to joints anchors, to update them during the game.
            Joint[] joints;
            List<Vector3> connectedAnchors = new List<Vector3>();
            List<Vector3> anchors = new List<Vector3>();
            joints = transform.GetComponentsInChildren<Joint>();

            Joint curJoint;
            for (int i = 0; i < joints.Length; i++)
            {
                curJoint = joints[i];
                connectedAnchors.Add(curJoint.connectedAnchor);
                anchors.Add(curJoint.anchor);
            }

            transform.localScale = new Vector3(PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale);

            // Update joints by resetting them to their original values
            Joint joint;
            for (int i = 0; i < joints.Length; i++)
            {
                joint = joints[i];
                joint.connectedAnchor = connectedAnchors[i];
                joint.anchor = anchors[i];
            }
        }

        #endregion

        #region Release intern

        public void SyncReleaseIntern(PlayerControllerB playerGrabberController)
        {
            // Make the pos slightly different so the interns separate on teleport
            Random randomInstance = new Random();
            Vector3 randomPos = new Vector3(playerGrabberController.transform.position.x + (float)randomInstance.NextDouble() * 0.1f,
                                            playerGrabberController.transform.position.y,
                                            playerGrabberController.transform.position.z + (float)randomInstance.NextDouble() * 0.1f);

            if (IsServer)
            {
                ReleaseInternClientRpc(playerGrabberController.playerClientId,
                                       randomPos,
                                       !playerGrabberController.isInsideFactory,
                                       isUsingEntrance: false);
            }
            else
            {
                ReleaseInternServerRpc(playerGrabberController.playerClientId,
                                       randomPos,
                                       !playerGrabberController.isInsideFactory,
                                       isUsingEntrance: false);
            }
        }

        [ServerRpc]
        private void ReleaseInternServerRpc(ulong idPlayerGrabberController,
                                            Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            ReleaseInternClientRpc(idPlayerGrabberController, pos, setOutside, isUsingEntrance);
        }

        [ClientRpc]
        private void ReleaseInternClientRpc(ulong idPlayerGrabberController,
                                            Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            ReleaseIntern(idPlayerGrabberController);
            TeleportIntern(pos, setOutside, isUsingEntrance);
        }

        private void ReleaseIntern(ulong idPlayerGrabberController)
        {
            if (idPlayerGrabberController == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                // Remove weight of body
                PlayerControllerB playerGrabberController = StartOfRound.Instance.allPlayerScripts[idPlayerGrabberController];
                float weightToLose = RagdollInternBody.GetWeight() - 1f < 0f ? 0f : RagdollInternBody.GetWeight() - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight - weightToLose, 1f, 10f);

                weightToLose = NpcController.Npc.carryWeight - 1f < 0f ? 0f : NpcController.Npc.carryWeight - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight - weightToLose, 1f, 10f);

                // Unregister held interns
                InternManager.Instance.UnregisterHeldInternForLocalPlayer((int)NpcController.Npc.playerClientId);
                InternManager.Instance.HideShowRagdollModel(NpcController.Npc, show: true);
            }

            if (HeldItem != null)
            {
                HeldItem.EnableItemMeshes(enable: true);
            }

            RagdollInternBody.Hide();

            // Enable model
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: true, disableLocalArms: true);

            // Set intern to follow
            SetCommandToFollowPlayer();
        }

        #endregion

        #region Spawn animation

        public bool IsSpawningAnimationRunning()
        {
            return spawnAnimationCoroutine != null;
        }

        public Coroutine BeginInternSpawnAnimation(EnumSpawnAnimation enumSpawnAnimation)
        {
            switch (enumSpawnAnimation)
            {
                case EnumSpawnAnimation.None:
                    return StartCoroutine(CoroutineNoSpawnAnimation());

                case EnumSpawnAnimation.OnlyPlayerSpawnAnimation:
                    return StartCoroutine(CoroutineOnlyPlayerSpawnAnimation());

                case EnumSpawnAnimation.RagdollFromDropShipAndPlayerSpawnAnimation:
                    return StartCoroutine(CoroutineFromDropShipAndPlayerSpawnAnimation());

                default:
                    return StartCoroutine(CoroutineNoSpawnAnimation());
            }
        }

        private IEnumerator CoroutineNoSpawnAnimation()
        {
            if (!IsOwner)
            {
                spawnAnimationCoroutine = null;
                yield break;
            }

            if (IsOwner)
            {
                // Change ai state
                SyncAssignTargetAndSetMovingTo(GetClosestIrlPlayer());
            }

            yield return null;

            if (IsOwner)
            {
                // Teleport again, cuz I don't know why the teleport does not work first time
                TeleportAgentAIAndBody(GameNetworkManager.Instance.localPlayerController.transform.position);
            }

            spawnAnimationCoroutine = null;
            yield break;
        }

        private IEnumerator CoroutineOnlyPlayerSpawnAnimation()
        {
            if (!IsOwner)
            {
                // Wait for spawn player animation
                yield return new WaitForSeconds(3f);
                NpcController.Npc.inSpecialInteractAnimation = false;
                spawnAnimationCoroutine = null;
                yield break;
            }

            UpdateInternSpecialAnimationValue(specialAnimation: true, timed: 0f, climbingLadder: false);
            NpcController.Npc.inSpecialInteractAnimation = true;
            NpcController.Npc.playerBodyAnimator.ResetTrigger("SpawnPlayer");
            NpcController.Npc.playerBodyAnimator.SetTrigger("SpawnPlayer");

            yield return new WaitForSeconds(3f);

            NpcController.Npc.inSpecialInteractAnimation = false;
            UpdateInternSpecialAnimationValue(specialAnimation: false, timed: 0f, climbingLadder: false);

            // Change ai state
            SyncAssignTargetAndSetMovingTo(GetClosestIrlPlayer());

            spawnAnimationCoroutine = null;
            yield break;
        }

        private IEnumerator CoroutineFromDropShipAndPlayerSpawnAnimation()
        {
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                // Wait for model replacement to add its component
                yield return new WaitForEndOfFrame();
                // Wait for  model replacement to init replacement models
                yield return new WaitForEndOfFrame();
            }

            animationCoroutineRagdollingRunning = true;
            PlayerControllerB closestPlayer = GetClosestIrlPlayer();

            // Spawn ragdoll
            InstantiateDeadBodyInfo(closestPlayer, GetRandomPushForce(InternManager.Instance.ItemDropShipPos + new Vector3(0, -1f, 0), NpcController.Npc.transform.position, 4f));
            RagdollInternBody.SetFreeRagdoll(ragdollBodyDeadBodyInfo);

            // Hide intern
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowReplacementModelOnlyBody?.Invoke(Npc, this, show: false);
            }
            else
            {
                InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
                HideShowLevelStickerBetaBadge(show: false);
            }

            yield return null;

            // Voice
            InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.Hit,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = false,
                IsInternInside = NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });

            // Wait in ragdoll state
            yield return new WaitForSeconds(2.5f);
            // End of ragdoll wait

            animationCoroutineRagdollingRunning = false;

            // Enable model
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowReplacementModelOnlyBody?.Invoke(Npc, this, show: true);
            }
            else
            {
                InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: true, disableLocalArms: true);
                HideShowLevelStickerBetaBadge(show: true);
            }

            // Hide ragdoll
            RagdollInternBody.Hide();

            if (!IsOwner)
            {
                // Wait for spawn player animation
                yield return new WaitForSeconds(3f);
                NpcController.Npc.inSpecialInteractAnimation = false;
                spawnAnimationCoroutine = null;
                yield break;
            }

            DeadBodyInfo? deadBodyInfo = RagdollInternBody.GetDeadBodyInfo();
            TeleportAgentAIAndBody(deadBodyInfo == null ? NpcController.Npc.transform.position : deadBodyInfo.transform.position);
            UpdateInternSpecialAnimationValue(specialAnimation: true, timed: 0f, climbingLadder: false);
            NpcController.Npc.inSpecialInteractAnimation = true;
            NpcController.Npc.playerBodyAnimator.ResetTrigger("SpawnPlayer");
            NpcController.Npc.playerBodyAnimator.SetTrigger("SpawnPlayer");

            // Wait in spawn player animation
            yield return new WaitForSeconds(3f);

            NpcController.Npc.inSpecialInteractAnimation = false;
            UpdateInternSpecialAnimationValue(specialAnimation: false, timed: 0f, climbingLadder: false);

            // Change ai state
            SyncAssignTargetAndSetMovingTo(closestPlayer);

            spawnAnimationCoroutine = null;
            yield break;
        }

        private PlayerControllerB GetClosestIrlPlayer()
        {
            PlayerControllerB closest = null!;
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!player.isPlayerControlled
                    || player.isPlayerDead)
                {
                    continue;
                }

                if (closest == null
                   || (player.transform.position - NpcController.Npc.transform.position).sqrMagnitude < (closest.transform.position - NpcController.Npc.transform.position).sqrMagnitude)
                {
                    closest = player;
                }
            }

            return closest;
        }

        private Vector3 GetRandomPushForce(Vector3 origin, Vector3 point, float forceMean)
        {
            point.y += UnityEngine.Random.Range(2f, 4f);

            //DrawUtil.DrawWhiteLine(LineRendererUtil.GetLineRenderer(), new Ray(origin, point - origin), Vector3.Distance(point, origin));
            float force = UnityEngine.Random.Range(forceMean * 0.5f, forceMean * 1.5f);
            return Vector3.Normalize(point - origin) * force / Vector3.Distance(point, origin);
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
        [ServerRpc(RequireOwnership = false)]
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
                PlayerControllerBHook.PlayJumpAudio_ReversePatch?.Invoke(NpcController.Npc);
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
        [ServerRpc(RequireOwnership = false)]
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
                StopSinkingState();
            }
        }

        public void StopSinkingState()
        {
            NpcController.Npc.isSinking = false;
            NpcController.Npc.statusEffectAudio.Stop();
            NpcController.Npc.voiceMuffledByEnemy = false;
            NpcController.Npc.sourcesCausingSinking = 0;
            NpcController.Npc.isMovementHindered = 0;
            NpcController.Npc.hinderedMultiplier = 1f;

            NpcController.Npc.isUnderwater = false;
            NpcController.Npc.underwaterCollider = null;
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

        #region Interns suits

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSuitInternServerRpc(ulong idInternController, int suitID)
        {
            ChangeSuitIntern(idInternController, suitID, playAudio: true);
            ChangeSuitInternClientRpc(idInternController, suitID);
        }

        [ClientRpc]
        private void ChangeSuitInternClientRpc(ulong idInternController, int suitID)
        {
            if (IsServer)
            {
                return;
            }

            ChangeSuitIntern(idInternController, suitID, playAudio: true);
        }

        public void ChangeSuitIntern(ulong idInternController, int suitID, bool playAudio = false)
        {
            if (suitID > StartOfRound.Instance.unlockablesList.unlockables.Count())
            {
                suitID = 0;
            }

            PlayerControllerB internController = StartOfRound.Instance.allPlayerScripts[idInternController];

            UnlockableSuit.SwitchSuitForPlayer(internController, suitID, playAudio);
            internController.thisPlayerModelArms.enabled = false;
            StartCoroutine(WaitSecondsForChangeSuitToApply());
            InternIdentity.SuitID = suitID;

            PluginLoggerHook.LogDebug?.Invoke($"Changed suit of intern {NpcController.Npc.playerUsername} to {suitID}: {StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName}");
        }

        public bool HasInternModelReplacementAPI()
        {
            return PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded ? ModelReplacementAPIHook.HasComponentModelReplacementAPI?.Invoke(NpcController.Npc.gameObject) ?? false : false;
        }

        private IEnumerator WaitSecondsForChangeSuitToApply()
        {
            yield return new WaitForSeconds(0.2f);

            NpcController.RefreshBillBoardPosition();

            IInternCullingBodyInfo? internCullingBodyInfo = InternManager.Instance.GetInternCullingBodyInfo(NpcController.Npc.gameObject);
            if (internCullingBodyInfo != null)
            {
                internCullingBodyInfo.HasModelReplacement = HasInternModelReplacementAPI();
            }

            yield break;
        }

        #endregion

        #region Emotes

        [ServerRpc(RequireOwnership = false)]
        public void StartPerformingEmoteInternServerRpc(int emoteID)
        {
            StartPerformingEmoteInternClientRpc(emoteID);
        }

        [ClientRpc]
        private void StartPerformingEmoteInternClientRpc(int emoteID)
        {
            NpcController.Npc.performingEmote = true;
            NpcController.Npc.playerBodyAnimator.SetInteger("emoteNumber", emoteID);
        }

        #endregion

        #region TooManyEmotes

        [ServerRpc(RequireOwnership = false)]
        public void PerformTooManyEmoteInternServerRpc(int tooManyEmoteID)
        {
            PerformTooManyInternClientRpc(tooManyEmoteID);
        }

        [ClientRpc]
        private void PerformTooManyInternClientRpc(int tooManyEmoteID)
        {
            NpcController.PerformTooManyEmote(tooManyEmoteID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopPerformTooManyEmoteInternServerRpc()
        {
            StopPerformTooManyInternClientRpc();
        }

        [ClientRpc]
        private void StopPerformTooManyInternClientRpc()
        {
            NpcController.StopPerformingTooManyEmote();
        }

        #endregion

        #region Npc adapter

        public Vector3 GetBillBoardPosition(GameObject bodyModel)
        {
            return npcController.GetBillBoardPosition(bodyModel, Npc.usernameCanvas.transform.localPosition);
        }

        #endregion

        public float GetAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
        {
            return this.AngleFOVWithLocalPlayerTimedCheck.GetAngleFOVWithLocalPlayer(localPlayerCameraTransform, internBodyPos);
        }

        public class TimedTouchingGroundCheck
        {
            private bool isTouchingGround = true;
            private RaycastHit groundHit;

            private long timer = 200 * TimeSpan.TicksPerMillisecond;
            private long lastTimeCalculate;

            public bool IsTouchingGround(Vector3 internPosition)
            {
                if (!NeedToRecalculate())
                {
                    return isTouchingGround;
                }

                CalculateTouchingGround(internPosition);
                return isTouchingGround;
            }

            public RaycastHit GetGroundHit(Vector3 internPosition)
            {
                if (!NeedToRecalculate())
                {
                    return groundHit;
                }

                CalculateTouchingGround(internPosition);
                return groundHit;
            }

            private bool NeedToRecalculate()
            {
                long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
                if (elapsedTime > timer)
                {
                    lastTimeCalculate = DateTime.Now.Ticks;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void CalculateTouchingGround(Vector3 internPosition)
            {
                isTouchingGround = Physics.Raycast(new Ray(internPosition + Vector3.up, -Vector3.up),
                                                   out groundHit,
                                                   2.5f,
                                                   StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            }
        }

        public class TimedAngleFOVWithLocalPlayerCheck
        {
            private float angle;

            private long timer = 50 * TimeSpan.TicksPerMillisecond;
            private long lastTimeCalculate;

            public float GetAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
            {
                if (!NeedToRecalculate())
                {
                    return angle;
                }

                CalculateAngleFOVWithLocalPlayer(localPlayerCameraTransform, internBodyPos);
                return angle;
            }

            private bool NeedToRecalculate()
            {
                long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
                if (elapsedTime > timer)
                {
                    lastTimeCalculate = DateTime.Now.Ticks;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void CalculateAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
            {
                angle = Vector3.Angle(localPlayerCameraTransform.forward, internBodyPos - localPlayerCameraTransform.position);
            }
        }
    }
}