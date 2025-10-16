using GameNetcodeStuff;
using LethalInternship.Core.Interns;
using LethalInternship.Core.Interns.AI;
using LethalInternship.Core.Interns.AI.Batches;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.PointsOfInterest;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Adapters;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Events;
using LethalInternship.SharedAbstractions.Hooks.BunkbedReviveHooks;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.MoreCompanyHooks;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks;
using LethalInternship.SharedAbstractions.Hooks.ShipTeleporterHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.Core.Managers
{
    /// <summary>
    /// Manager responsible for spawning, initializing, managing interns and synchronize clients.
    /// </summary>
    /// <remarks>
    /// For spawning interns, the managers resize the <c>allPlayerScripts</c>, <c>allPlayerObjects</c> by adding the number of interns max 
    /// from <see cref="Const.INTERN_AVAILABLE_MAX"><c>Const.INTERN_AVAILABLE_MAX</c></see>.<br/>
    /// An intern is a <c>PlayerControllerB</c> with an <c>InternAI</c>, both attached to the <c>GameObject</c> of the playerController.<br/>
    /// So the manager instantiate new playerControllers (body) and spawn on server new AI (brain) and link them together.<br/>
    /// Other methods in class can retrieve the brain from the body index and vice versa with the use of arrays.<br/>
    /// <br/>
    /// Important points:<br/>
    /// The <c>PlayerControllerB</c> instantiated for interns do not spawn on server, they are synchronized in each client.<br/>
    /// This means that the <c>PlayerControllerB</c> of an intern is never owned, only the <c>InternAI</c> associated is.<br/>
    /// The patches for the original game code need to always look for an <c>InternAI</c> associated with <c>PlayerControllerB</c> they encounter.<br/>
    /// Typically, everything that happens to the player owner of his body (real player), should function the same to the body of an intern owned by this player,
    /// the local player.<br/>
    /// <br/>
    /// Note: To be compatible with MoreCompany, the manager need to keep reference of the number of "real" players initialize by the game and the mod (MoreCompany)
    /// Typically 4 (base game) + 28 (default from MoreCompany)<br/>
    /// MoreCompany resize arrays in the same way, after each scene load, so quite a number of time, the manager execute after MoreCompany and resize the arrays to the
    /// right size : 4 + 28 + 16 (default for LethalInternship)<br/>
    /// </remarks>
    public class InternManager : NetworkBehaviour, IInternManager
    {
        public static InternManager Instance { get; private set; } = null!;
        public GameObject ManagerGameObject => this.gameObject;

        /// <summary>
        /// Size of allPlayerScripts, AllPlayerObjects, for normal players controller + interns player controllers
        /// </summary>
        public int AllEntitiesCount => allEntitiesCount;
        /// <summary>
        /// Integer corresponding to the first player controller associated with an intern in StartOfRound.Instance.allPlayerScripts
        /// </summary>
        public int IndexBeginOfInterns
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Length - (AllInternAIs?.Length ?? 0);
            }
        }
        public List<int> HeldInternsLocalPlayer { get => heldInternsLocalPlayer; set => heldInternsLocalPlayer = value; }
        public new bool IsServer => base.IsServer;
        public VehicleController? VehicleController { get => vehicleController; }
        public List<IBodyReplacementBase> ListBodyReplacementOnDeadBodies { get => listBodyReplacementOnDeadBodies; set => listBodyReplacementOnDeadBodies = value; }

        private int allEntitiesCount;
        public bool LandingStatusAllowed;
        private List<int> heldInternsLocalPlayer = new List<int>();

        public Vector3 ItemDropShipPos { get => itemDropShipPos; set => itemDropShipPos = value; }
        private Vector3 itemDropShipPos;

        private VehicleController? vehicleController;
        public RagdollGrabbableObject[] RagdollInternBodies = null!;
        public TimedOrderedInternBodiesDistanceListCheck OrderedInternDistanceListTimedCheck = null!;
        public List<IInternCullingBodyInfo> InternBodiesSpawned = null!;
        public IInternCullingBodyInfo[] OrderedInternBodiesInFOV = new IInternCullingBodyInfo[PluginRuntimeProvider.Context.Config.MaxInternsAvailable * 2];
        private List<IBodyReplacementBase> listBodyReplacementOnDeadBodies = new List<IBodyReplacementBase>();

        public Dictionary<EnemyAI, INoiseListener> DictEnemyAINoiseListeners { get => dictEnemyAINoiseListeners; }
        private Dictionary<EnemyAI, INoiseListener> dictEnemyAINoiseListeners = new Dictionary<EnemyAI, INoiseListener>();

        private IInternAI[] AllInternAIs = null!;
        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        private Coroutine registerItemsCoroutine = null!;
        private Coroutine BeamOutInternsCoroutine = null!;
        private ClientRpcParams ClientRpcParams = new ClientRpcParams();

        private float timerAnimationCulling;
        private float timerNoAnimationAfterLag;
        private float timerIsAnInternScheduledToLand;
        private bool isAnInternScheduledToLand;

        private float timerRegisterAINoiseListener;
        private List<EnemyAI> ListEnemyAINonNoiseListeners = new List<EnemyAI>();
        public Dictionary<string, int> DictTagSurfaceIndex = new Dictionary<string, int>();

        private List<IPointOfInterest> listPointOfInterest = new List<IPointOfInterest>();

        private float timerSetInternInElevator;

        /// <summary>
        /// Initialize instance,
        /// repopulate pool of interns if InternManager reset when loading game
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.IsSpawned && Instance.IsServer)
                {
                    Instance.NetworkObject.Despawn(destroy: true);
                }
                else
                {
                    Destroy(Instance.gameObject);
                }
            }

            Instance = this;
            if (PluginEventsProvider.Events != null)
            {
                PluginEventsProvider.Events.InitialSyncCompleted += Config_InitialSyncCompleted;
            }
            PluginLoggerHook.LogDebug?.Invoke($"Client {NetworkManager.LocalClientId}, MaxInternsAvailable before CSync {PluginRuntimeProvider.Context.Config.MaxInternsAvailable}");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!base.NetworkManager.IsServer)
            {
                // Destroy local manager
                Destroy(InternManagerProvider.Instance.ManagerGameObject);

                // Use manager from server
                InternManagerProvider.Instance = this;
                Instance = this;
            }
        }

        private void Config_InitialSyncCompleted(object sender, EventArgs e)
        {
            if (IsHost)
            {
                return;
            }

            PluginLoggerHook.LogDebug?.Invoke($"Client {NetworkManager.LocalClientId}, ManagePoolOfInterns after CSync, MaxInternsAvailable {PluginRuntimeProvider.Context.Config.MaxInternsAvailable}");
            ManagePoolOfInterns();
        }

        private void FixedUpdate()
        {
            RegisterAINoiseListener(Time.fixedDeltaTime);
        }

        private void RegisterAINoiseListener(float deltaTime)
        {
            timerRegisterAINoiseListener += deltaTime;
            if (timerRegisterAINoiseListener < 1f)
            {
                return;
            }

            timerRegisterAINoiseListener = 0f;
            RoundManager instanceRM = RoundManager.Instance;
            foreach (EnemyAI spawnedEnemy in instanceRM.SpawnedEnemies)
            {
                if (ListEnemyAINonNoiseListeners.Contains(spawnedEnemy))
                {
                    continue;
                }
                else if (DictEnemyAINoiseListeners.ContainsKey(spawnedEnemy))
                {
                    continue;
                }

                INoiseListener noiseListener;
                if (spawnedEnemy.gameObject.TryGetComponent<INoiseListener>(out noiseListener))
                {
                    PluginLoggerHook.LogDebug?.Invoke($"new enemy noise listener, spawnedEnemy {spawnedEnemy}");
                    DictEnemyAINoiseListeners.Add(spawnedEnemy, noiseListener);
                }
                else
                {
                    PluginLoggerHook.LogDebug?.Invoke($"new enemy not noise listener, spawnedEnemy {spawnedEnemy}");
                    ListEnemyAINonNoiseListeners.Add(spawnedEnemy);
                }
            }
        }

        public void RegisterItems()
        {
            if (registerItemsCoroutine == null)
            {
                registerItemsCoroutine = StartCoroutine(RegisterItemsCoroutine());
            }
        }

        private IEnumerator RegisterItemsCoroutine()
        {
            if (HoarderBugAI.grabbableObjectsInMap == null)
            {
                yield break;
            }

            HoarderBugAI.grabbableObjectsInMap.Clear();
            yield return null;

            GrabbableObject[] array = Object.FindObjectsOfType<GrabbableObject>();
            PluginLoggerHook.LogDebug?.Invoke($"Intern register grabbable object, found : {array.Length}");
            for (int i = 0; i < array.Length; i++)
            {
                GrabbableObject grabbableObject = array[i];
                if (!grabbableObject.grabbableToEnemies || grabbableObject.deactivated)
                {
                    continue;
                }

                Vector3 floorPosition;
                bool inShipRoom = false;
                bool inElevator = false;
                floorPosition = grabbableObject.GetItemFloorPosition(default(Vector3));
                if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(floorPosition))
                {
                    inElevator = true;
                    inShipRoom = true;
                }
                else if (StartOfRound.Instance.shipBounds.bounds.Contains(floorPosition))
                {
                    inElevator = true;
                }
                GameNetworkManager.Instance.localPlayerController.SetItemInElevator(inElevator, inShipRoom, grabbableObject);

                HoarderBugAI.grabbableObjectsInMap.Add(grabbableObject.gameObject);
                yield return null;
            }

            registerItemsCoroutine = null!;
            yield break;
        }

        private void Start()
        {
            // Identities
            IdentityManager.Instance.InitIdentities(PluginRuntimeProvider.Context.Config.ConfigIdentities.configIdentities);

            // Intern objects
            if (PluginRuntimeProvider.Context.PluginIrlPlayersCount > 0)
            {
                // only resize if irl players not 0, which means we already tried to populate pool of interns
                // But the manager somehow reset
                ManagePoolOfInterns();
            }

            // Load data from save
            SaveManager.Instance.LoadAllDataFromSave();

            // Init footstep surfaces tags
            DictTagSurfaceIndex.Clear();
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                DictTagSurfaceIndex.Add(StartOfRound.Instance.footstepSurfaces[i].surfaceTag, i);
            }

            OrderedInternDistanceListTimedCheck = new TimedOrderedInternBodiesDistanceListCheck();
            InternBodiesSpawned = new List<IInternCullingBodyInfo>();
        }

        private void Update()
        {
            timerAnimationCulling += Time.deltaTime;
            if (timerAnimationCulling > 0.01f)
            {
                timerAnimationCulling = 0;
                UpdateAnimationsCulling();
            }

            timerIsAnInternScheduledToLand += Time.deltaTime;
            if (timerIsAnInternScheduledToLand > 1f)
            {
                timerIsAnInternScheduledToLand = 0f;
                isAnInternScheduledToLand = IdentityManager.Instance.IsAnIdentityToDrop();
            }

            ProcessCalculatePathQueue();
        }

        /// <summary>
        /// Initialize, resize and populate allPlayerScripts, allPlayerObjects with new interns
        /// </summary>
        public void ManagePoolOfInterns()
        {
            StartOfRound instance = StartOfRound.Instance;
            int maxInternsPossible = PluginRuntimeProvider.Context.Config.MaxInternsAvailable;

            if (instance.allPlayerObjects[3].gameObject == null)
            {
                PluginLoggerHook.LogInfo?.Invoke("No player objects initialized in game, aborting interns initializations.");
                return;
            }

            if (PluginRuntimeProvider.Context.PluginIrlPlayersCount == 0)
            {
                PluginRuntimeProvider.Context.PluginIrlPlayersCount = instance.allPlayerObjects.Length;
                PluginLoggerHook.LogDebug?.Invoke($"PluginIrlPlayersCount = {PluginRuntimeProvider.Context.PluginIrlPlayersCount}");
            }

            int irlPlayersCount = PluginRuntimeProvider.Context.PluginIrlPlayersCount;
            int irlPlayersAndInternsCount = irlPlayersCount + maxInternsPossible;

            // Initialize back ups
            if (AllPlayerObjectsBackUp == null)
            {
                AllInternAIs = new InternAI[maxInternsPossible];
                AllPlayerObjectsBackUp = new GameObject[maxInternsPossible];
                AllPlayerScriptsBackUp = new PlayerControllerB[maxInternsPossible];

                RagdollInternBodies = new RagdollGrabbableObject[irlPlayersAndInternsCount];
            }
            else if (AllPlayerObjectsBackUp.Length != maxInternsPossible)
            {
                Array.Resize(ref AllInternAIs, maxInternsPossible);
                Array.Resize(ref AllPlayerObjectsBackUp, maxInternsPossible);
                Array.Resize(ref AllPlayerScriptsBackUp, maxInternsPossible);

                Array.Resize(ref RagdollInternBodies, irlPlayersAndInternsCount);
            }

            allEntitiesCount = irlPlayersAndInternsCount;
            // Need to populate pool of interns ?
            if (instance.allPlayerScripts.Length == AllEntitiesCount)
            {
                // the arrays have not been resize between round
                PluginLoggerHook.LogInfo?.Invoke($"Pool of interns ok. The arrays have not been resized, PluginIrlPlayersCount: {PluginRuntimeProvider.Context.PluginIrlPlayersCount}, arrays length: {instance.allPlayerScripts.Length}");
                return;
            }

            // Interns
            ResizePoolOfInterns(irlPlayersAndInternsCount);
            PopulatePoolOfInterns(irlPlayersCount);
            UpdateSoundManagerWithInterns(irlPlayersAndInternsCount);
        }

        /// <summary>
        /// Resize <c>allPlayerScripts</c>, <c>allPlayerObjects</c> by adding <see cref="Config.MaxInternsAvailable"><c>Config.MaxInternsAvailable</c></see>
        /// </summary>
        /// <param name="irlPlayersCount">Number of "real" players, 4 without morecompany, for calculating resizing</param>
        private void ResizePoolOfInterns(int irlPlayersAndInternsCount)
        {
            StartOfRound instance = StartOfRound.Instance;
            var previousSize = instance.allPlayerObjects.Length;

            Array.Resize(ref instance.allPlayerObjects, irlPlayersAndInternsCount);
            Array.Resize(ref instance.allPlayerScripts, irlPlayersAndInternsCount);
            Array.Resize(ref instance.gameStats.allPlayerStats, irlPlayersAndInternsCount);
            Array.Resize(ref instance.playerSpawnPositions, irlPlayersAndInternsCount);
            PluginLoggerHook.LogDebug?.Invoke($"Resized arrays from {previousSize} to {irlPlayersAndInternsCount}");
        }

        /// <summary>
        /// Populate allPlayerScripts, allPlayerObjects with new controllers, instantiated of the 4th player, initiated and named
        /// </summary>
        /// <param name="irlPlayersCount">Number of "real" players, 4 base game (without morecompany), for calculating parameterization</param>
        private void PopulatePoolOfInterns(int irlPlayersCount)
        {
            PluginLoggerHook.LogDebug?.Invoke($"Attempt to populate pool of interns. irlPlayersCount {irlPlayersCount}");
            StartOfRound instance = StartOfRound.Instance;
            GameObject internObjectParent = instance.allPlayerObjects[3];

            // Using back up if available,
            // If the size of array has been modified by morecompany for example when loading scene or the game, at some point
            for (int i = 0; i < AllPlayerObjectsBackUp.Length; i++)
            {
                // Back ups ?
                int indexPlusIrlPlayersCount = i + irlPlayersCount;
                if (AllPlayerObjectsBackUp[i] != null)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"PopulatePoolOfInterns - use of backup : {AllPlayerScriptsBackUp[i].playerUsername}");
                    instance.allPlayerObjects[indexPlusIrlPlayersCount] = AllPlayerObjectsBackUp[i];
                    instance.allPlayerScripts[indexPlusIrlPlayersCount] = AllPlayerScriptsBackUp[i];
                    instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                    instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];
                    continue;
                }

                GameObject internObject = Object.Instantiate<GameObject>(internObjectParent, internObjectParent.transform.parent);

                // Body
                PlayerControllerB internController = internObject.GetComponentInChildren<PlayerControllerB>();
                internController.playerClientId = (ulong)(indexPlusIrlPlayersCount);
                internController.isPlayerDead = false;
                internController.isPlayerControlled = false;
                internController.transform.localScale = new Vector3(PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale);
                internController.thisController.radius *= PluginRuntimeProvider.Context.Config.InternSizeScale;
                internController.actualClientId = internController.playerClientId + Const.INTERN_ACTUAL_ID_OFFSET;
                internController.playerUsername = string.Format(ConfigConst.DEFAULT_INTERN_NAME, internController.playerClientId - (ulong)irlPlayersCount);

                // Radar
                instance.mapScreen.radarTargets.Add(new TransformAndName(internController.transform, internController.playerUsername, false));

                // Skins
                UnlockableSuit.SwitchSuitForPlayer(internController, 0, false);
                if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
                {
                    ModelReplacementAPIHook.RemovePlayerModelReplacementFromController?.Invoke(internController);
                }
                if (PluginRuntimeProvider.Context.IsModMoreCompanyLoaded)
                {
                    MoreCompanyHook.RemoveCosmetics?.Invoke(internController);
                }

                instance.allPlayerObjects[indexPlusIrlPlayersCount] = internObject;
                instance.allPlayerScripts[indexPlusIrlPlayersCount] = internController;
                instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];

                AllPlayerObjectsBackUp[i] = internObject;
                AllPlayerScriptsBackUp[i] = internController;

                internObject.SetActive(false);
            }

            PluginLoggerHook.LogInfo?.Invoke("Pool of interns populated.");
        }

        public void ResetIdentities()
        {
            IdentityManager.Instance.InitIdentities(PluginRuntimeProvider.Context.Config.ConfigIdentities.configIdentities);
        }

        private void UpdateSoundManagerWithInterns(int irlPlayersAndInternsCount)
        {
            SoundManager instanceSM = SoundManager.Instance;

            Array.Resize(ref instanceSM.playerVoicePitchLerpSpeed, irlPlayersAndInternsCount);
            Array.Resize(ref instanceSM.playerVoicePitchTargets, irlPlayersAndInternsCount);
            Array.Resize(ref instanceSM.playerVoicePitches, irlPlayersAndInternsCount);
            Array.Resize(ref instanceSM.playerVoiceVolumes, irlPlayersAndInternsCount);

            // From moreCompany
            for (int i = IndexBeginOfInterns; i < irlPlayersAndInternsCount; i++)
            {
                instanceSM.playerVoicePitchLerpSpeed[i] = 3f;
                instanceSM.playerVoicePitchTargets[i] = 1f;
                instanceSM.playerVoicePitches[i] = 1f;
                instanceSM.playerVoiceVolumes[i] = 0.5f;
            }

            ResizePlayerVoiceMixers(irlPlayersAndInternsCount);
        }

        public void ResizePlayerVoiceMixers(int irlPlayersAndInternsCount)
        {
            // From moreCompany
            SoundManager instanceSM = SoundManager.Instance;
            Array.Resize<AudioMixerGroup>(ref instanceSM.playerVoiceMixers, irlPlayersAndInternsCount);
            AudioMixerGroup audioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault((AudioMixerGroup x) => x.name.StartsWith("VoicePlayer"));
            for (int i = IndexBeginOfInterns; i < irlPlayersAndInternsCount; i++)
            {
                instanceSM.playerVoiceMixers[i] = audioMixerGroup;
            }
        }

        #region Spawn Intern

        /// <summary>
        /// Rpc method on server spawning network object from intern prefab and calling the client
        /// </summary>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnInternServerRpc(SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console");
                return;
            }

            int identityID = -1;
            // Get selected identities
            int[] selectedIdentities = IdentityManager.Instance.GetIdentitiesToDrop();
            if (selectedIdentities.Length > 0)
            {
                identityID = selectedIdentities[0];
            }

            if (identityID < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"Try to spawn intern, no more intern identities available.");
                return;
            }

            //IdentityManager.Instance.InternIdentities[identityID].Status = EnumStatusIdentity.Spawned;
            spawnInternsParamsNetworkSerializable.InternIdentityID = identityID;
            SpawnInternServer(spawnInternsParamsNetworkSerializable);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnThisInternServerRpc(int identityID, SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console.");
                return;
            }

            if (identityID < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"Failed to spawn specific intern identity with id {identityID}.");
                return;
            }

            spawnInternsParamsNetworkSerializable.InternIdentityID = identityID;
            SpawnInternServer(spawnInternsParamsNetworkSerializable);
        }

        private void SpawnInternServer(SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            int indexNextPlayerObject = GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"No more intern can be spawned at the same time, see MaxInternsAvailable value : {PluginRuntimeProvider.Context.Config.MaxInternsAvailable}");
                return;
            }
            int indexNextIntern = indexNextPlayerObject - IndexBeginOfInterns;

            NetworkObject networkObject;
            IInternAI internAI = AllInternAIs[indexNextIntern];
            if (internAI == null
                || internAI.NetworkObject == null)
            {
                // Or spawn one (server only)
                GameObject internPrefab = Object.Instantiate<GameObject>(PluginRuntimeProvider.Context.InternNPCPrefab.enemyPrefab);
                internAI = internPrefab.GetComponent<IInternAI>();
                AllInternAIs[indexNextIntern] = internAI;

                networkObject = internPrefab.GetComponentInChildren<NetworkObject>();
                networkObject.Spawn(true);
            }
            else
            {
                // Use internAI if exists
                networkObject = AllInternAIs[indexNextIntern].NetworkObject;
            }

            // Get an identity for the intern
            internAI.InternIdentity = IdentityManager.Instance.InternIdentities[spawnInternsParamsNetworkSerializable.InternIdentityID];

            // Choose suit
            int suitID;
            if (PluginRuntimeProvider.Context.Config.ChangeSuitAutoBehaviour)
            {
                suitID = GameNetworkManager.Instance.localPlayerController.currentSuitID;
            }
            else
            {
                suitID = internAI.InternIdentity.SuitID ?? internAI.InternIdentity.GetRandomSuitID();
            }

            // Spawn ragdoll dead bodies of intern
            NetworkObject networkObjectRagdollBody = SpawnRagdollBodies((int)StartOfRound.Instance.allPlayerScripts[indexNextPlayerObject].playerClientId);

            // Send to client to spawn intern
            spawnInternsParamsNetworkSerializable.IndexNextIntern = indexNextIntern;
            spawnInternsParamsNetworkSerializable.IndexNextPlayerObject = indexNextPlayerObject;
            spawnInternsParamsNetworkSerializable.InternIdentityID = internAI.InternIdentity.IdIdentity;
            spawnInternsParamsNetworkSerializable.SuitID = suitID;

            //SpawnInternClientRpc(networkObject, networkObjectRagdollBody, spawnInternsParamsNetworkSerializable);
            StartCoroutine(WaitBeforeSpawnOnClient(networkObject, networkObjectRagdollBody, spawnInternsParamsNetworkSerializable));
        }

        IEnumerator WaitBeforeSpawnOnClient(NetworkObjectReference networkObjectReferenceInternAI,
                                            NetworkObjectReference networkObjectReferenceRagdollInternBody,
                                            SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            yield return null;
            SpawnInternClientRpc(networkObjectReferenceInternAI, networkObjectReferenceRagdollInternBody, spawnParamsNetworkSerializable);
        }

        /// <summary>
        /// Get the index of the next <c>PlayerControllerB</c> not controlled and ready to be hooked to an <c>InternAI</c>
        /// </summary>
        /// <returns></returns>
        private int GetNextAvailablePlayerObject()
        {
            StartOfRound instance = StartOfRound.Instance;
            for (int i = IndexBeginOfInterns; i < instance.allPlayerScripts.Length; i++)
            {
                if (!instance.allPlayerScripts[i].isPlayerControlled)
                {
                    return i;
                }
            }
            return -1;
        }

        private NetworkObject SpawnRagdollBodies(int playerClientId)
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            NetworkObject networkObjectRagdoll;

            // Spawn grabbable ragdoll intern body of intern
            RagdollGrabbableObject? ragdollInternBody = RagdollInternBodies[playerClientId];
            if (ragdollInternBody == null)
            {
                GameObject gameObject = Object.Instantiate<GameObject>(instanceSOR.ragdollGrabbableObjectPrefab, instanceSOR.propsContainer);
                networkObjectRagdoll = gameObject.GetComponent<NetworkObject>();
                networkObjectRagdoll.Spawn(false);
                ragdollInternBody = gameObject.GetComponent<RagdollGrabbableObject>();
                ragdollInternBody.bodyID.Value = Const.INIT_RAGDOLL_ID;
                RagdollInternBodies[playerClientId] = ragdollInternBody;
            }
            else
            {
                networkObjectRagdoll = ragdollInternBody.gameObject.GetComponent<NetworkObject>();
                if (!networkObjectRagdoll.IsSpawned)
                {
                    networkObjectRagdoll.Spawn(false);
                }
            }
            return networkObjectRagdoll;
        }

        /// <summary>
        /// Client side, when receiving <c>NetworkObjectReference</c> for the <c>InternAI</c> spawned on server,
        /// adds it to its corresponding arrays
        /// </summary>
        /// <param name="networkObjectReferenceInternAI"><c>NetworkObjectReference</c> for the <c>InternAI</c> spawned on server</param>
        /// <param name="indexNextIntern">Corresponding index in <c>AllInternAIs</c> for the body <c>GameObject</c> at another index in <c>allPlayerObjects</c></param>
        /// <param name="indexNextPlayerObject">Corresponding index in <c>allPlayerObjects</c> for the body of intern</param>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        [ClientRpc]
        private void SpawnInternClientRpc(NetworkObjectReference networkObjectReferenceInternAI,
                                          NetworkObjectReference networkObjectReferenceRagdollInternBody,
                                          SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            PluginLoggerHook.LogInfo?.Invoke($"Client receive RPC to spawn intern... position : {spawnParamsNetworkSerializable.SpawnPosition}, yRot: {spawnParamsNetworkSerializable.YRot}");

            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console");
                return;
            }

            // Get internAI from server
            networkObjectReferenceInternAI.TryGet(out NetworkObject networkObjectInternAI);
            InternAI internAI = networkObjectInternAI.gameObject.GetComponent<InternAI>();
            AllInternAIs[spawnParamsNetworkSerializable.IndexNextIntern] = internAI;

            // Get ragdoll body from server
            networkObjectReferenceRagdollInternBody.TryGet(out NetworkObject networkObjectRagdollGrabbableObject);
            RagdollGrabbableObject ragdollBody = networkObjectRagdollGrabbableObject.gameObject.GetComponent<RagdollGrabbableObject>();

            // Check for identites correctness
            if (spawnParamsNetworkSerializable.InternIdentityID >= IdentityManager.Instance.InternIdentities.Length)
            {
                IdentityManager.Instance.ExpandWithNewDefaultIdentities(numberToAdd: 1);
            }

            InitInternSpawning(internAI, ragdollBody,
                               spawnParamsNetworkSerializable);
        }

        /// <summary>
        /// Initialize intern by initializing body (<c>PlayerControllerB</c>) and brain (<c>InternAI</c>) to default values.
        /// Attach the brain to the body, attach <c>InternAI</c> <c>Transform</c> to the <c>GameObject</c> of the <c>PlayerControllerB</c>.
        /// </summary>
        /// <param name="internAI"><c>InternAI</c> to initialize</param>
        /// <param name="indexNextPlayerObject">Corresponding index in <c>allPlayerObjects</c> for the body of intern</param>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        private void InitInternSpawning(InternAI internAI, RagdollGrabbableObject ragdollBody,
                                        SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            StartOfRound instance = StartOfRound.Instance;
            IInternIdentity internIdentity = IdentityManager.Instance.InternIdentities[spawnParamsNetworkSerializable.InternIdentityID];

            GameObject objectParent = instance.allPlayerObjects[spawnParamsNetworkSerializable.IndexNextPlayerObject];
            objectParent.transform.position = spawnParamsNetworkSerializable.SpawnPosition;
            objectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, spawnParamsNetworkSerializable.YRot, 0f));

            PlayerControllerB internController = instance.allPlayerScripts[spawnParamsNetworkSerializable.IndexNextPlayerObject];
            internController.playerUsername = internIdentity.Name;
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.playerActions = new PlayerActions();
            internController.health = PluginRuntimeProvider.Context.Config.InternMaxHealth;
            DisableInternControllerModel(objectParent, internController, enable: true, disableLocalArms: true);
            internController.isInsideFactory = !spawnParamsNetworkSerializable.IsOutside;
            internController.isMovementHindered = 0;
            internController.hinderedMultiplier = 1f;
            internController.criticallyInjured = false;
            internController.bleedingHeavily = false;
            internController.activatingItem = false;
            internController.twoHanded = false;
            internController.inSpecialInteractAnimation = false;
            internController.freeRotationInInteractAnimation = false;
            internController.disableSyncInAnimation = false;
            internController.disableLookInput = false;
            internController.inAnimationWithEnemy = null;
            internController.holdingWalkieTalkie = false;
            internController.speakingToWalkieTalkie = false;
            internController.isSinking = false;
            internController.isUnderwater = false;
            internController.sinkingValue = 0f;
            internController.sourcesCausingSinking = 0;
            internController.isClimbingLadder = false;
            internController.setPositionOfDeadPlayer = false;
            internController.mapRadarDotAnimator.SetBool(Const.MAPDOT_ANIMATION_BOOL_DEAD, false);
            internController.externalForceAutoFade = Vector3.zero;
            internController.voiceMuffledByEnemy = false;
            internController.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, false);
            internController.climbSpeed = Const.CLIMB_SPEED;
            internController.usernameBillboardText.enabled = true;

            FieldInfo fieldInfo = typeof(PlayerControllerB).GetField("updatePositionForNewlyJoinedClient", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fieldInfo.SetValue(internController, true);

            // CleanLegsFromMoreEmotesMod
            CleanLegsFromMoreEmotesMod(internController);

            // internAI
            internAI.InternId = Array.IndexOf(AllInternAIs, internAI);
            internAI.creatureAnimator = internController.playerBodyAnimator;
            internAI.AdaptController(internController);
            internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");
            internAI.InternIdentity = internIdentity;
            internAI.InternIdentity.Hp = spawnParamsNetworkSerializable.Hp == 0 ? PluginRuntimeProvider.Context.Config.InternMaxHealth : spawnParamsNetworkSerializable.Hp;
            internAI.InternIdentity.SuitID = spawnParamsNetworkSerializable.SuitID;
            internAI.InternIdentity.Status = EnumStatusIdentity.Spawned;
            internAI.SetEnemyOutside(spawnParamsNetworkSerializable.IsOutside);

            // Attach ragdoll body
            internAI.RagdollInternBody = new RagdollInternBody(ragdollBody);

            // Plug ai on intern body
            internAI.enabled = false;
            internAI.NetworkObject.AutoObjectParentSync = false;
            internAI.transform.parent = objectParent.transform;
            internAI.NetworkObject.AutoObjectParentSync = true;
            internAI.enabled = true;

            objectParent.SetActive(true);

            // Unsuscribe from events to prevent double trigger
            PlayerControllerBHook.OnDisable_ReversePatch?.Invoke(internController);

            // Destroy dead body of identity
            if (spawnParamsNetworkSerializable.ShouldDestroyDeadBody)
            {
                if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded
                    && internIdentity.BodyReplacementBase != null)
                {
                    ModelReplacementAPIHook.RemovePlayerModelReplacement?.Invoke(internIdentity.BodyReplacementBase);
                    internIdentity.BodyReplacementBase = null;
                }
                if (internIdentity.DeadBody != null)
                {
                    Object.Destroy(internIdentity.DeadBody.gameObject);
                    internIdentity.DeadBody = null;
                }
            }
            // Remove deadbody on controller
            if (internController.deadBody != null)
            {
                internController.deadBody = null;
            }

            // Register body for animation culling
            RegisterInternBodyForAnimationCulling(internController);

            // Switch suit
            internAI.ChangeSuitIntern(internController.playerClientId, internAI.InternIdentity.SuitID.Value);

            // Show model replacement
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowModelReplacement?.Invoke(internAI.Npc, show: true);
            }

            // Radar name update
            foreach (var radarTarget in instance.mapScreen.radarTargets)
            {
                if (radarTarget != null
                    && radarTarget.transform == internController.transform)
                {
                    radarTarget.name = internController.playerUsername;
                    break;
                }
            }

            // Init intern
            PluginLoggerHook.LogDebug?.Invoke($"++ Intern with body {internController.playerClientId} with identity spawned: {internIdentity.ToString()}");
            internAI.Init((EnumSpawnAnimation)spawnParamsNetworkSerializable.enumSpawnAnimation);
        }

        /// <summary>
        /// Manual DisablePlayerModel, for compatibility with mod LethalPhones, does not trigger patch of DisablePlayerModel in LethalPhones
        /// </summary>
        public void DisableInternControllerModel(GameObject internObject, PlayerControllerB internController, bool enable = false, bool disableLocalArms = false)
        {
            HideShowInternControllerModel(internObject, enable);
            if (disableLocalArms)
            {
                internController.thisPlayerModelArms.enabled = false;
            }
        }

        private void CleanLegsFromMoreEmotesMod(PlayerControllerB internController)
        {
            GameObject? gameObject = internController.playerBodyAnimator.transform.Find("FistPersonLegs")?.gameObject;
            if (gameObject != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{internController.playerUsername}: Cleaning legs from more emotes");
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        #endregion

        #region SpawnInternsFromDropShip

        /// <summary>
        /// Spawn intern from dropship after opening the doors, all around the dropship
        /// </summary>
        /// <param name="spawnPositions">Positions of spawn for interns</param>
        public void SpawnInternsFromDropShip(Transform[] spawnPositions)
        {
            StartCoroutine(SpawnInternsCoroutine(spawnPositions));
        }

        private IEnumerator SpawnInternsCoroutine(Transform[] spawnPositions)
        {
            yield return null;
            int pos = 0;
            int nbInternsToDropShip = IdentityManager.Instance.GetNbIdentitiesToDrop();
            for (int i = 0; i < nbInternsToDropShip; i++)
            {
                if (pos > 3)
                {
                    pos = 0;
                }
                Transform transform = spawnPositions[pos++];
                SpawnInternServerRpc(new SpawnInternsParamsNetworkSerializable()
                {
                    enumSpawnAnimation = (int)EnumSpawnAnimation.RagdollFromDropShipAndPlayerSpawnAnimation,
                    SpawnPosition = transform.position,
                    YRot = transform.eulerAngles.y,
                    IsOutside = true
                });

                yield return new WaitForSeconds(0.3f);
            }
        }

        #endregion

        #region Point of interests

        public bool CheckAndClearInvalidPointOfInterest(IPointOfInterest? pointOfInterest)
        {
            if (pointOfInterest != null && pointOfInterest.IsInvalid)
            {
                listPointOfInterest.Remove(pointOfInterest);
                return true;
            }

            return false;
        }

        public IPointOfInterest GetPointOfInterestOrDefaultInterestPoint(Vector3 pos)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == pos);
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new DefaultInterestPoint(pos));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        public IPointOfInterest GetPointOfInterestOrVehicleInterestPoint(VehicleController vehicleController)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == VehicleInterestPoint.GetVehiclePoint(vehicleController));
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new VehicleInterestPoint(vehicleController));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        public IPointOfInterest GetPointOfInterestOrShipInterestPoint(Transform shipTransform)
        {
            IPointOfInterest? pointOfInterest = listPointOfInterest.FirstOrDefault(x => x.GetPoint() == ShipInterestPoint.GetShipPoint(shipTransform));
            if (pointOfInterest != null)
            {
                return pointOfInterest;
            }

            pointOfInterest = new PointOfInterest();
            pointOfInterest.TryAddInterestPoint(new ShipInterestPoint(shipTransform));
            listPointOfInterest.Add(pointOfInterest);
            return pointOfInterest;
        }

        #endregion

        #region Teleporters

        public void TeleportOutInterns(ShipTeleporter teleporter,
                                       Random shipTeleporterSeed)
        {
            if (this.BeamOutInternsCoroutine != null)
            {
                base.StopCoroutine(this.BeamOutInternsCoroutine);
            }
            this.BeamOutInternsCoroutine = base.StartCoroutine(this.BeamOutInterns(teleporter, shipTeleporterSeed));
        }

        private IEnumerator BeamOutInterns(ShipTeleporter teleporter,
                                           Random shipTeleporterSeed)
        {
            yield return new WaitForSeconds(5f);

            if (StartOfRound.Instance.inShipPhase)
            {
                yield break;
            }

            Vector3 positionIntern;
            Vector3 teleportPos;
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.isEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.RagdollInternBody.IsRagdollBodyHeld())
                {
                    continue;
                }

                positionIntern = internAI.NpcController.Npc.transform.position;
                if (internAI.NpcController.Npc.deadBody != null)
                {
                    positionIntern = internAI.NpcController.Npc.deadBody.bodyParts[5].transform.position;
                }

                if ((positionIntern - teleporter.teleportOutPosition.position).sqrMagnitude > 2f * 2f)
                {
                    continue;
                }

                if (RoundManager.Instance.insideAINodes.Length == 0)
                {
                    continue;
                }

                // Random pos
                teleportPos = RoundManager.Instance.insideAINodes[shipTeleporterSeed.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                teleportPos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(teleportPos, 10f, default(NavMeshHit), shipTeleporterSeed, -1);

                // Teleport intern
                ShipTeleporterHook.SetPlayerTeleporterId_ReversePatch?.Invoke(teleporter, internAI.NpcController.Npc, 2);
                internAI.TeleportIntern(teleportPos, setOutside: false, isUsingEntrance: false);
                internAI.NpcController.Npc.beamOutParticle.Play();
                teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterBeamUpSFX);
            }
        }

        #endregion

        /// <summary>
        /// Are interns bought and ready to drop on moon ?
        /// </summary>
        /// <returns><c>true</c> if a positive number of interns are scheduled to dropship on moon, <c>false</c> otherwise or on company moon</returns>
        public bool AreInternsScheduledToLand()
        {
            // no drop of interns on company building moon
            if (StartOfRound.Instance.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID)
            {
                return false;
            }

            return isAnInternScheduledToLand && LandingStatusAllowed;
        }

        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB</c> <c>playerClientId</c>
        /// </summary>
        /// <param name="playerClientId"><c>playerClientId</c> of <c>PlayerControllerB</c></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated, else returns null</returns>
        public IInternAI? GetInternAI(int playerClientId)
        {
            if (IndexBeginOfInterns == StartOfRound.Instance.allPlayerScripts.Length
                || playerClientId < IndexBeginOfInterns)
            {
                // Real player
                return null;
            }

            return AllInternAIs[playerClientId - IndexBeginOfInterns];
        }

        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB.playerClientId</c>, 
        /// only if the local client calling the method is the owner of <c>InternAI</c>
        /// </summary>
        /// <param name="index"></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated and the local client is the owner, 
        /// else returns null</returns>
        public IInternAI? GetInternAIIfLocalIsOwner(int index)
        {
            IInternAI? internAI = GetInternAI(index);
            if (internAI != null
                && internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
            {
                return internAI;
            }

            return null;
        }

        /// <summary>
        /// Get the <c>InternAI</c> that hold the <c>GrabbableObject</c>
        /// </summary>
        /// <param name="grabbableObject">Object held by the <c>InternAI</c> the method is looking for</param>
        /// <returns><c>InternAI</c> if holding the <c>GrabbableObject</c>, else returns null</returns>
        public IInternAI? GetInternAiOwnerOfObject(GrabbableObject grabbableObject)
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.HeldItem == null)
                {
                    continue;
                }

                if (internAI.HeldItem == grabbableObject)
                {
                    return internAI;
                }
            }

            return null;
        }

        public IInternAI[] GetInternsAiHoldByPlayer(int idPlayerHolder)
        {
            List<IInternAI> results = new List<IInternAI>();

            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.RagdollInternBody.IsRagdollBodyHeldByPlayer(idPlayerHolder))
                {
                    results.Add(internAI);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Is the <c>PlayerControllerB.playerClientId</c> corresponding to an index of a intern in <c>allPlayerScripts</c>, 
        /// a <c>PlayerControllerB</c> that has <c>InternAI</c>
        /// </summary>
        /// <param name="id"><c>PlayerControllerB.playerClientId</c></param>
        /// <returns><c>true</c> if <c>PlayerControllerB</c> has <c>InternAI</c>, else <c>false</c></returns>
        public bool IsIdPlayerIntern(int id)
        {
            return id >= IndexBeginOfInterns;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> corresponding to an intern in <c>allPlayerScripts</c>, 
        /// a <c>PlayerControllerB</c> that has <c>InternAI</c>
        /// </summary>
        /// <returns><c>true</c> if <c>PlayerControllerB</c> has <c>InternAI</c>, else <c>false</c></returns>
        public bool IsPlayerIntern(PlayerControllerB player)
        {
            if (player == null) return false;
            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            return internAI != null;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> the local player or the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> that is an intern
        /// </remarks>
        /// <param name="player"></param>
        public bool IsPlayerLocalOrInternOwnerLocal(PlayerControllerB player)
        {
            if (player == null)
            {
                return false;
            }
            if (player == GameNetworkManager.Instance.localPlayerController)
            {
                return true;
            }

            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        /// <summary>
        /// Is the collider a <c>PlayerControllerB</c> that is the local player, or an intern that is owned by the local player ?
        /// </summary>
        public bool IsColliderFromLocalOrInternOwnerLocal(Collider collider)
        {
            PlayerControllerB player = collider.gameObject.GetComponent<PlayerControllerB>();
            return IsPlayerLocalOrInternOwnerLocal(player);
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> who is an intern
        /// </remarks>
        /// <param name="player"></param>
        public bool IsPlayerInternOwnerLocal(PlayerControllerB player)
        {
            if (player == null)
            {
                return false;
            }

            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB.playerClientId</c> the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> that is an intern
        /// </remarks>
        /// <param name="idPlayer"><c>playerClientId</c> of <c>PlayerControllerB</c></param>
        public bool IsIdPlayerInternOwnerLocal(int idPlayer)
        {
            IInternAI? internAI = GetInternAI(idPlayer);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        public bool IsPlayerInternControlledAndOwner(PlayerControllerB player)
        {
            return IsPlayerInternOwnerLocal(player) && player.isPlayerControlled;
        }

        public bool IsAnInternAiOwnerOfObject(GrabbableObject grabbableObject)
        {
            IInternAI? internAI = GetInternAiOwnerOfObject(grabbableObject);
            if (internAI == null)
            {
                return false;
            }

            return true;
        }

        public IInternAI[] GetInternsAIOwnedByLocal()
        {
            return AllInternAIs.Where(x => x != null
                                        && x.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
                               .ToArray();
        }

        public void SetInternsInElevatorLateUpdate(float deltaTime)
        {
            timerSetInternInElevator += deltaTime;
            if (timerSetInternInElevator < 0.5f)
            {
                return;
            }
            timerSetInternInElevator = 0f;

            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null)
                {
                    continue;
                }

                internAI.SetInternInElevator();
            }
        }

        public bool IsLocalPlayerNextToChillInterns()
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsLocalPlayerHoldingInterns()
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.RagdollInternBody != null
                    && internAI.RagdollInternBody.IsRagdollBodyHeldByPlayer((int)GameNetworkManager.Instance.localPlayerController.playerClientId))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetDamageFromSlimeIfIntern(PlayerControllerB player)
        {
            if (IsPlayerIntern(player))
            {
                return 5;
            }

            return 35;
        }

        public int MaxHealthPercent(int percentage, int maxHealth)
        {
            int healthPercent = (int)(((double)percentage / (double)100) * (double)maxHealth);
            return healthPercent < 1 ? 1 : healthPercent;
        }

        #region SyncEndOfRoundInterns

        /// <summary>
        /// Only for the owner of <c>InternManager</c>, call server and clients to count intern left alive to re-drop on next round
        /// </summary>
        public void SyncEndOfRoundInterns()
        {
            if (!base.IsOwner)
            {
                return;
            }

            if (base.IsServer)
            {
                foreach (InternAI internAI in AllInternAIs)
                {
                    if (internAI == null
                        || internAI.isEnemyDead
                        || internAI.NpcController.Npc.isPlayerDead
                        || !internAI.NpcController.Npc.isPlayerControlled
                        || internAI.AreHandsFree())
                    {
                        continue;
                    }

                    internAI.DropItem();
                }

                SyncEndOfRoundInternsFromServerToClientRpc();
            }
            else
            {
                SyncEndOfRoundInternsFromClientToServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to count intern left alive to re-drop on next round
        /// </summary>
        [ServerRpc]
        private void SyncEndOfRoundInternsFromClientToServerRpc()
        {
            SyncEndOfRoundInternsFromServerToClientRpc();
        }

        /// <summary>
        /// Client side, count intern left alive to re-drop on next round
        /// </summary>
        [ClientRpc]
        private void SyncEndOfRoundInternsFromServerToClientRpc()
        {
            EndOfRoundForInterns();
        }

        private void EndOfRoundForInterns()
        {
            DictEnemyAINoiseListeners.Clear();
            ListEnemyAINonNoiseListeners.Clear();

            CountAliveAndDisableInterns();
        }

        /// <summary>
        /// Count and disable the interns still alive
        /// </summary>
        /// <returns>Number of interns still alive</returns>
        private void CountAliveAndDisableInterns()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            if (instanceSOR.currentLevel.levelID == 3)
            {
                return;
            }

            PlayerControllerB internController;
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || internAI.NpcController == null)
                {
                    continue;
                }

                internController = internAI.NpcController.Npc;

                DisableInternControllerModel(internController.gameObject, internController, enable: false, disableLocalArms: false);
                if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
                {
                    ModelReplacementAPIHook.RemoveInternModelReplacement?.Invoke(internAI, forceRemove: false);
                }

                if (internController.isPlayerDead
                    || !internController.isPlayerControlled)
                {
                    continue;
                }

                internController.isPlayerControlled = false;
                internController.localVisor.position = internController.playersManager.notSpawnedPosition.position;
                internController.transform.position = internController.playersManager.notSpawnedPosition.position;

                internAI.InternIdentity.Status = EnumStatusIdentity.ToDrop;
                instanceSOR.allPlayerObjects[internController.playerClientId].SetActive(false);
            }

            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.CleanListBodyReplacementOnDeadBodies?.Invoke();
            }

            if (HeldInternsLocalPlayer != null)
            {
                HeldInternsLocalPlayer.Clear();
            }
        }

        #endregion

        #region Vehicle landing on map RPC

        public void VehicleHasLanded()
        {
            vehicleController = Object.FindObjectOfType<VehicleController>();
            PluginLoggerHook.LogDebug?.Invoke($"Vehicle has landed : {vehicleController}");
        }

        #endregion

        #region Config RPC

        [ServerRpc(RequireOwnership = false)]
        public void SyncLoadedJsonIdentitiesServerRpc(ulong clientId)
        {
            PluginLoggerHook.LogDebug?.Invoke($"Client {clientId} ask server/host {NetworkManager.LocalClientId} to SyncLoadedJsonIdentities");
            ClientRpcParams.Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientId }
            };

            SyncLoadedJsonIdentitiesClientRpc(
                new ConfigIdentitiesNetworkSerializable()
                {
                    ConfigIdentities = PluginRuntimeProvider.Context.Config.ConfigIdentities.configIdentities.ToArray()
                },
                ClientRpcParams);
        }

        [ClientRpc]
        private void SyncLoadedJsonIdentitiesClientRpc(ConfigIdentitiesNetworkSerializable configIdentityNetworkSerializable,
                                                       ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            PluginLoggerHook.LogInfo?.Invoke($"Client {NetworkManager.LocalClientId} : sync json interns identities");
            PluginLoggerHook.LogDebug?.Invoke($"Loaded {configIdentityNetworkSerializable.ConfigIdentities.Length} identities from server");
            foreach (ConfigIdentity configIdentity in configIdentityNetworkSerializable.ConfigIdentities)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{configIdentity.ToString()}");
            }

            PluginLoggerHook.LogDebug?.Invoke($"Recreate identities for {configIdentityNetworkSerializable.ConfigIdentities.Length} interns");
            IdentityManager.Instance.InitIdentities(configIdentityNetworkSerializable.ConfigIdentities.ToArray());
        }

        #endregion

        #region Voices

        public void UpdateAllInternsVoiceEffects()
        {
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.isEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.creatureVoice == null)
                {
                    continue;
                }

                internAI.UpdateInternVoiceEffects();
            }
        }

        public bool DidAnInternJustTalkedClose(int idInternTryingToTalk)
        {
            IInternAI internTryingToTalk = AllInternAIs[idInternTryingToTalk];

            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI == internTryingToTalk)
                {
                    continue;
                }

                if (internAI.InternIdentity.Voice.IsTalking()
                    && (internAI.NpcController.Npc.transform.position - internTryingToTalk.NpcController.Npc.transform.position).sqrMagnitude < VoicesConst.DISTANCE_HEAR_OTHER_INTERNS * VoicesConst.DISTANCE_HEAR_OTHER_INTERNS)
                {
                    return true;
                }
            }

            return false;
        }

        public void SyncPlayAudioIntern(int internID, string smallPathAudioClip)
        {
            AllInternAIs[internID].PlayAudioServerRpc(smallPathAudioClip, PluginRuntimeProvider.Context.Config.Talkativeness);
        }

        public void PlayAudibleNoiseForIntern(int internID,
                                              Vector3 noisePosition,
                                              float noiseRange = 10f,
                                              float noiseLoudness = 0.5f,
                                              int noiseID = 0)
        {
            IInternAI internAI = AllInternAIs[internID];
            bool noiseIsInsideClosedShip = internAI.NpcController.Npc.isInHangarShipRoom && internAI.NpcController.Npc.playersManager.hangarDoorsClosed;
            internAI.NpcController.PlayAudibleNoiseIntern(noisePosition,
                                                          noiseRange,
                                                          noiseLoudness,
                                                          timesPlayedInSameSpot: 0,
                                                          noiseIsInsideClosedShip,
                                                          noiseID);
        }

        #endregion

        #region Animations culling

        private void UpdateAnimationsCulling()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                return;
            }

            if (timerNoAnimationAfterLag > 0f)
            {
                timerNoAnimationAfterLag += Time.deltaTime;
                if (timerNoAnimationAfterLag > 3f)
                {
                    timerNoAnimationAfterLag = 0f;
                }
                return;
            }

            if (timerNoAnimationAfterLag > 0f)
            {
                // No animation allowed
                List<IInternCullingBodyInfo> orderedInternBodiesDistanceListToDisable = OrderedInternDistanceListTimedCheck.GetOrderedInternDistanceList(InternBodiesSpawned);
                foreach (IInternCullingBodyInfo internCullingBodyInfo in orderedInternBodiesDistanceListToDisable)
                {
                    // Cut animation
                    internCullingBodyInfo.ResetBodyInfos();
                }
                return;
            }

            // Stop animation if we are losing frames
            if (timerNoAnimationAfterLag <= 0f && Time.deltaTime > 0.125f)
            {
                timerNoAnimationAfterLag += Time.deltaTime;
                return;
            }

            Array.Fill(OrderedInternBodiesInFOV, null);

            int indexAnyModel = 0;
            int indexNoModelReplacement = 0;
            int indexWithModelReplacement = 0;

            int indexAnyModelInFOV = 0;
            int indexNoModelReplacementInFOV = 0;
            int indexWithModelReplacementInFOV = 0;

            List<IInternCullingBodyInfo> orderedInternBodiesDistanceList = OrderedInternDistanceListTimedCheck.GetOrderedInternDistanceList(InternBodiesSpawned);
            foreach (IInternCullingBodyInfo internCullingBodyInfo in orderedInternBodiesDistanceList)
            {
                // Cut animation before deciding which intern can animate
                internCullingBodyInfo.ResetBodyInfos();

                internCullingBodyInfo.RankDistanceAnyModel = indexAnyModel++;
                if (internCullingBodyInfo.CheckIsInFOV())
                {
                    if (internCullingBodyInfo.HasModelReplacement)
                    {
                        internCullingBodyInfo.RankDistanceWithModelReplacementInFOV = indexWithModelReplacementInFOV++;
                    }
                    else
                    {
                        internCullingBodyInfo.RankDistanceNoModelReplacementInFOV = indexNoModelReplacementInFOV++;
                    }

                    internCullingBodyInfo.RankDistanceAnyModelInFOV = indexAnyModelInFOV;
                    internCullingBodyInfo.BodyInFOV = true;
                    OrderedInternBodiesInFOV[indexAnyModelInFOV] = internCullingBodyInfo;
                    indexAnyModelInFOV++;
                }

                // In or not in FOV
                if (internCullingBodyInfo.HasModelReplacement)
                {
                    internCullingBodyInfo.RankDistanceWithModelReplacement = indexWithModelReplacement++;
                }
                else
                {
                    internCullingBodyInfo.RankDistanceNoModelReplacement = indexNoModelReplacement++;
                }
            }

            timerAnimationCulling = 0f;
        }

        public void RegisterInternBodyForAnimationCulling(Component internBody, bool hasModelReplacement = false)
        {
            // Clean
            InternBodiesSpawned.RemoveAll(x => x.InternBody == null);

            // Register or re-init
            IInternCullingBodyInfo? internCullingBodyInfo = InternBodiesSpawned.FirstOrDefault(x => x.InternBody == internBody);
            if (internCullingBodyInfo == null)
            {
                InternBodiesSpawned.Add(new InternCullingBodyInfo(internBody, hasModelReplacement));
            }
            else
            {
                internCullingBodyInfo.Init(hasModelReplacement);
            }

            // Resizing, bodies info contains player controllers and ragdoll corpse
            if (InternBodiesSpawned.Count > OrderedInternBodiesInFOV.Length)
            {
                Array.Resize(ref OrderedInternBodiesInFOV, InternBodiesSpawned.Count);
            }
        }

        public IInternCullingBodyInfo? GetInternCullingBodyInfo(GameObject gameObject)
        {
            foreach (IInternCullingBodyInfo internCullingBodyInfo in InternBodiesSpawned)
            {
                if (internCullingBodyInfo == null
                    || internCullingBodyInfo.InternBody == null)
                {
                    continue;
                }

                if (internCullingBodyInfo.InternBody.gameObject == gameObject)
                {
                    return internCullingBodyInfo;
                }
            }

            return null;
        }

        public void RegisterHeldInternForLocalPlayer(int idInternController)
        {
            if (heldInternsLocalPlayer == null)
            {
                heldInternsLocalPlayer = new List<int>();
            }

            heldInternsLocalPlayer.Add(idInternController);
        }

        public void UnregisterHeldInternForLocalPlayer(int idInternController)
        {
            if (HeldInternsLocalPlayer == null)
            {
                return;
            }

            HeldInternsLocalPlayer.Remove(idInternController);
        }

        public void HideShowRagdollModel(PlayerControllerB internController, bool show)
        {
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowRagdollWithModelReplacement?.Invoke(internController.gameObject, show);
            }
            else
            {
                HideShowInternControllerModel(internController.gameObject, show);
            }
        }

        public void HideShowInternControllerModel(GameObject internObject, bool show)
        {
            SkinnedMeshRenderer[] componentsInChildren = internObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = show;
            }
        }

        #endregion

        #region BunkbedMod RPC

        [ServerRpc(RequireOwnership = false)]
        public void UpdateReviveCountServerRpc(int id)
        {
            UpdateReviveCountClientRpc(id);
        }

        [ClientRpc]
        private void UpdateReviveCountClientRpc(int id)
        {
            BunkbedReviveHook.UpdateReviveCount?.Invoke(id);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncGroupCreditsForNotOwnerTerminalServerRpc(int newGroupCredits, int numItemsInShip)
        {
            Terminal terminalScript = TerminalManager.Instance.GetTerminal();
            terminalScript.SyncGroupCreditsServerRpc(newGroupCredits, numItemsInShip);
        }

        #endregion

        #region ReviveCompany mod RPC

        [ServerRpc(RequireOwnership = false)]
        public void UpdateReviveCompanyRemainingRevivesServerRpc(string identityName)
        {
            UpdateReviveCompanyRemainingRevivesClientRpc(identityName);
        }

        [ClientRpc]
        private void UpdateReviveCompanyRemainingRevivesClientRpc(string identityName)
        {
            ReviveCompanyHook.UpdateReviveCompanyRemainingRevives?.Invoke(identityName);
        }

        #endregion

        #region Graph and path calculation

        private TimedGetGraphEntrances getGraphEntrancesTimed = null!;

        private int nextInstructionGroupId = 1;
        public int GetNewInstructionGroupId() => nextInstructionGroupId++;

        public GraphController? GetGraphEntrances()
        {
            if (getGraphEntrancesTimed == null)
            {
                getGraphEntrancesTimed = new TimedGetGraphEntrances();
            }

            return getGraphEntrancesTimed.GetGraphEntrances();
        }

        private int maxBatchesPerFrame = 1;
        private int maxInstructionsPerFrame = 1;
        private int currentBatch = -2;
        private float fairnessBoost = 5000f; // the weight of a second of waiting

        private Dictionary<int, BatchRequest> activeBatches = new Dictionary<int, BatchRequest>();

        public void RequestBatch(int idBatch, List<IInstruction> instructions, Action? onBatchComplete = null)
        {
            var newBatch = new BatchRequest(idBatch, instructions, onBatchComplete);
            activeBatches[idBatch] = newBatch;
        }

        private void ProcessCalculatePathQueue()
        {
            currentBatch = -2;
            if (activeBatches.Count == 0) return;

            int processedBatches = 0;
            int processedInstructions = 0;

            var sorted = activeBatches.Values
                        .OrderBy(b => GetDistanceFromClosestPlayer(b))
                        .ToList();

            foreach (var batch in sorted)
            {
                if (processedBatches >= maxBatchesPerFrame) break;
                if (processedInstructions >= maxInstructionsPerFrame) break;

                // Has remaining instructions ?
                if (!batch.HasRemaining)
                {
                    activeBatches.Remove(batch.id);
                    continue;
                }

                // Execute one instruction only
                var instr = batch.CurrentInstruction;
                ExecuteInstruction(instr);
                batch.Advance();

                processedInstructions++;
                processedBatches++;
                currentBatch = batch.id;

                if (!batch.HasRemaining)
                {
                    batch.onBatchComplete?.Invoke();
                    CancelBatch(batch.id);
                }
            }
        }

        public void CancelGroup(int idBatch, int groupId)
        {
            if (activeBatches.TryGetValue(idBatch, out var batch))
            {
                batch.CancelInstructionsInGroup(groupId);

                if (!batch.HasRemaining)
                    activeBatches.Remove(idBatch);
            }
        }

        public void CancelGroupGlobal(int groupId)
        {
            var toRemove = new List<int>();
            foreach (var kvp in activeBatches)
            {
                kvp.Value.CancelInstructionsInGroup(groupId);
                if (!kvp.Value.HasRemaining)
                    toRemove.Add(kvp.Key);
            }
            foreach (var idBatch in toRemove)
                activeBatches.Remove(idBatch);
        }

        public void CancelBatch(int idBatch)
        {
            activeBatches.Remove(idBatch);
        }

        public int GetCurrentBatch()
        {
            return currentBatch;
        }

        private void ExecuteInstruction(IInstruction instr)
        {
            instr.Execute();
        }

        private float GetDistanceFromClosestPlayer(BatchRequest batch)
        {
            if (!batch.HasRemaining) return float.MaxValue;
            if (batch.id < 0) return float.MinValue;

            IInternAI? internAI = GetInternAI(batch.id);
            if (internAI == null)
            {
                return float.MaxValue;
            }

            return internAI.GetClosestPlayerDistance();
        }

        #endregion

        #region Items global

        private TimedGetGrabbableObjectsList getGrabbableObjectsListTimed = null!;

        /// <summary>
        /// Dictionnary of the recently dropped object on the ground.
        /// The intern will not try to grab them for a certain time (<see cref="Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS"><c>Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS</c></see>).
        /// </summary>
        public Dictionary<GrabbableObject, float> DictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        public void AddToDictJustDroppedItems(GrabbableObject grabbableObject)
        {
            DictJustDroppedItems[grabbableObject] = Time.realtimeSinceStartup;
        }

        public bool IsGrabbableObjectJustDropped(GrabbableObject grabbableObject)
        {
            if (DictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Trim dictionnary if too large, trim only the dropped item since a long time
        /// </summary>
        public void TrimDictJustDroppedItems()
        {
            if (DictJustDroppedItems != null && DictJustDroppedItems.Count > 20)
            {
                PluginLoggerHook.LogDebug?.Invoke($"TrimDictJustDroppedItems Count{DictJustDroppedItems.Count}");
                var itemsToClean = DictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                                                       .Select(x => x.Key)
                                                       .ToList();
                foreach (var item in itemsToClean)
                {
                    DictJustDroppedItems.Remove(item);
                }
            }
        }

        public List<GameObject> GetGrabbableObjectsList()
        {
            if (getGrabbableObjectsListTimed == null)
            {
                getGrabbableObjectsListTimed = new TimedGetGrabbableObjectsList();
            }

            return getGrabbableObjectsListTimed.GetGrabbableObjectsList();
        }

        #endregion
    }
}
