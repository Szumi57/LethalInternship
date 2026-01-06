using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.BT;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Adapters;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
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
    public partial class InternAI : EnemyAI, IInternAI
    {
        public INpcController NpcController => npcController;
        public PlayerControllerB Npc => npcController.Npc;
        public IInternIdentity InternIdentity { get => internIdentity; set => internIdentity = value; }

        public GameObject GameObject => this.gameObject;
        public new ulong OwnerClientId => base.OwnerClientId;
        public new NetworkObject NetworkObject => base.NetworkObject;
        public Transform Transform => this.transform;

        public IRagdollInternBody RagdollInternBody { get => ragdollInternBody; set => ragdollInternBody = value; }
        public bool IsEnemyDead => base.isEnemyDead;
        public new bool IsSpawned => base.IsSpawned;
        public bool AnimationCoroutineRagdollingRunning => animationCoroutineRagdollingRunning;
        public List<IBodyReplacementBase> ListModelReplacement { get => listModelReplacement; set => listModelReplacement = value; }

        private INpcController npcController = null!;
        private IInternIdentity internIdentity = null!;
        private IRagdollInternBody ragdollInternBody = null!;
        public int InternId = -1;

        public Collider InternBodyCollider = null!;
        private List<IBodyReplacementBase> listModelReplacement = null!;
        private Dictionary<string, Component> dictComponentByCollider = null!;
        private EnumStateControllerMovement StateControllerMovement;
        private float updateDestinationIntervalInternAI;

        public LineRendererUtil LineRendererUtil = null!;

        private void Awake()
        {
            // Behaviour states
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
            EntrancesTeleportArray = FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Doors
            doorLocksArray = FindObjectsByType<DoorLock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Important colliders
            InitImportantColliders();

            // Model replacements
            listModelReplacement = new List<IBodyReplacementBase>();

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
            this.PointOfInterest = null;

            // Body collider
            InternBodyCollider = NpcController.Npc.GetComponentInChildren<Collider>();

            // Intern voice
            InitInternVoiceComponent();
            UpdateInternVoiceEffects();

            // Weapon item holder
            if (WeaponHolderTransform == null)
            {
                GameObject weaponHolderGameObject = new GameObject("InternWeaponHolder");
                Transform parentWeaponHolder = NpcController.Npc.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003");
                weaponHolderGameObject.transform.SetParent(parentWeaponHolder);
                weaponHolderGameObject.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                WeaponHolderTransform = weaponHolderGameObject.transform;
            }

            // Load items (only weapon for now)
            // After init of WeaponHolderTransform
            if (base.IsServer)
            {
                if (internIdentity.ItemsInInventory.Length > 0)
                {
                    int itemID = internIdentity.ItemsInInventory[0];
                    if (itemID <= StartOfRound.Instance.allItemsList.itemsList.Count)
                    {
                        GameObject gameObject = Object.Instantiate<GameObject>(StartOfRound.Instance.allItemsList.itemsList[itemID].spawnPrefab, StartOfRound.Instance.propsContainer);
                        GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
                        gameObject.GetComponent<NetworkObject>().Spawn(false);

                        SpawnWeaponToHoldWhenSpawningClientRpc(grabbableObject.NetworkObject);
                    }
                }
            }

            // Line renderer used for debugging stuff
            LineRendererUtil = new LineRendererUtil(20, transform);

            TeleportAgentAIAndBody(NpcController.Npc.transform.position);
            StateControllerMovement = EnumStateControllerMovement.FollowAgent;

            // Start timed calculation
            IsTouchingGroundTimedCheck = new TimedTouchingGroundCheck();
            AngleFOVWithLocalPlayerTimedCheck = new TimedAngleFOVWithLocalPlayerCheck();

            // Spawn animation
            spawnAnimationCoroutine = BeginInternSpawnAnimation(enumSpawnAnimation);
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

            BridgeTrigger[] bridgeTriggers = FindObjectsByType<BridgeTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

        [ClientRpc]
        private void SpawnWeaponToHoldWhenSpawningClientRpc(NetworkObjectReference norGrabbableObject)
        {
            norGrabbableObject.TryGet(out NetworkObject networkObjectRagdollGrabbableObject);
            GrabbableObject grabbableObject = networkObjectRagdollGrabbableObject.gameObject.GetComponent<GrabbableObject>();
            grabbableObject.fallTime = 0f;

            this.GrabItem(grabbableObject);
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
                agent.speed = PluginRuntimeProvider.Context.Config.InternSpeed * vector2.magnitude;

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

                // Stop the free falling through the map
                if (NpcController.Npc.transform.position.y < -500f)
                {
                    var closestNode = allAINodes.OrderBy(node => (node.transform.position - NpcController.Npc.transform.position).sqrMagnitude)
                                      .FirstOrDefault();
                    if (closestNode != null)
                    {
                        TeleportAgentAIAndBody(closestNode.transform.position);
                    }
                    else
                    {
                        TeleportAgentAIAndBody(this.GetClosestIrlPlayer().transform.position);
                    }
                }
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

            if (ShouldDoAIInterval())
            {
                DoAIInterval();
            }
        }

        private bool ShouldDoAIInterval()
        {
            // Update interval timer for AI calculation
            if (updateDestinationIntervalInternAI >= 0f)
            {
                updateDestinationIntervalInternAI -= Time.deltaTime;
                return false;
            }

            if (isEnemyDead
                || NpcController.Npc.isPlayerDead
                || (RagdollInternBody != null && RagdollInternBody.IsRagdollBodyHeld()))
            {
                return false;
            }

            if (IsSpawningAnimationRunning())
            {
                return false;
            }

            if (InternManager.Instance.GetCurrentBatch() == (int)Npc.playerClientId)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"Current batch for intern ! id {Npc.playerClientId}");
                return false;
            }

            // Reset time
            updateDestinationIntervalInternAI = AIIntervalTime;
            return true;
        }

        /// <summary>
        /// Where the AI begin its calculations.
        /// </summary>
        /// <remarks>
        /// For the behaviour of the AI, we use a behaviour tree
        /// </remarks>
        public override void DoAIInterval()
        {
            SetAgent(enabled: true);

            BTController.TickTree(AIIntervalTime);

            // Doors
            OpenDoorIfNeeded();
        }

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
            if (IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position))
            {
                RaycastHit raycastHit = IsTouchingGroundTimedCheck.GetGroundHit(NpcController.Npc.thisPlayerBody.position);
                if (raycastHit.collider != null
                    && dictComponentByCollider.TryGetValue(raycastHit.collider.name, out Component component))
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
            }

            if (NpcController.Npc.externalForces.y > 7.1f)
            {
                IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position, forceCalculation: true);
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} externalForces.y {NpcController.Npc.externalForces.y} is touching ground {IsTouchingGroundTimedCheck.IsTouchingGround(NpcController.Npc.thisPlayerBody.position)}");
                return true;
            }

            return false;
        }

        public void FollowCrouchIfCanDo(bool panik = false)
        {
            if (panik
                && NpcController.Npc.isCrouching)
            {
                NpcController.OrderToToggleCrouch();
                return;
            }

            if (PluginRuntimeProvider.Context.Config.FollowCrouchWithPlayer
                && targetPlayer != null)
            {
                if (targetPlayer.isCrouching
                    && !NpcController.Npc.isCrouching)
                {
                    NpcController.OrderToToggleCrouch();
                }
                else if (!targetPlayer.isCrouching
                        && NpcController.Npc.isCrouching)
                {
                    NpcController.OrderToToggleCrouch();
                }
            }
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
                || collidedEnemy.GetType() == typeof(InternAI))
            {
                return;
            }

            if (collidedEnemy.GetType() == typeof(FlowerSnakeEnemy))
            {
                // FlowerSnakeEnemy collide with the intern collider
                collidedEnemy.OnCollideWithPlayer(InternBodyCollider);
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
    }
}