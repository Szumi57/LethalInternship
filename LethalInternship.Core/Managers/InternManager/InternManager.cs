using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Events;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.MoreCompanyHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
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
    public partial class InternManager : NetworkBehaviour, IInternManager
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

        private int allEntitiesCount;

        private IInternAI[] AllInternAIs = null!;
        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

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
            CheckAnimationsCulling();

            CheckIsAnInternScheduledToLand();

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
    }
}
