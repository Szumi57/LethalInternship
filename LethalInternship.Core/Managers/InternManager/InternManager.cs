using GameNetcodeStuff;
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
        public int AllEntitiesCount => PluginRuntimeProvider.Context.AllEntitiesCount;
        /// <summary>
        /// Integer corresponding to the first player controller associated with an intern in StartOfRound.Instance.allPlayerScripts
        /// </summary>
        public int IndexBeginOfInterns => PluginRuntimeProvider.Context.PluginIrlPlayersCount;
        public List<int> HeldInternsLocalPlayer { get => heldInternsLocalPlayer; set => heldInternsLocalPlayer = value; }
        public new bool IsServer => base.IsServer;

        private IInternAI[] AllInternAIs = null!;
        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this)
            {
                this.NetworkObject.Despawn(true);
                return;
            }

            Instance = this;
            InternManagerProvider.Register(this);

            // Inits
            if (PluginEventsProvider.Events != null)
            {
                PluginEventsProvider.Events.InitialSyncCompleted += Config_InitialSyncCompleted;
            }

            // On client connected
            if (!this.IsServer && !this.IsHost)
            {
                SyncLoadedJsonIdentitiesServerRpc(this.NetworkManager.LocalClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
            {
                Instance = null!;
                InternManagerProvider.Unregister(this);
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

        private float poolLogTimer;
        private void Update()
        {
            CheckAnimationsCulling();

            CheckIsAnInternScheduledToLand();

            ProcessCalculatePathQueue();

            //poolLogTimer += Time.deltaTime;
            //if (poolLogTimer >= 2f)
            //{
            //    poolLogTimer -= 2f;
            //    Debug.Log("Pools.LogStats -------------------");
            //    Pools.LogStats();
            //    Debug.Log("----------------------------------");
            //}
        }

        public void Init()
        {
            PluginLoggerHook.LogInfo?.Invoke("Initializing InternManager...");

            // Intern objects
            ManagePoolOfInterns();

            // Init footstep surfaces tags
            DictTagSurfaceIndex.Clear();
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                DictTagSurfaceIndex.Add(StartOfRound.Instance.footstepSurfaces[i].surfaceTag, i);
            }

            OrderedInternDistanceListTimedCheck = new TimedOrderedInternBodiesDistanceListCheck();
            InternBodiesSpawned = new List<IInternCullingBodyInfo>();
            listPointOfInterest = new List<IPointOfInterest>();

            // Managers
            UIManager.Instance.InitUI(HUDManager.Instance.HUDContainer.transform.parent);
            AudioManager.Instance.Init();
            InputManager.Instance.Init();
        }

        /// <summary>
        /// Initialize, resize and populate allPlayerScripts, allPlayerObjects with new interns
        /// </summary>
        public void ManagePoolOfInterns()
        {
            AllInternAIs ??= new IInternAI[AllEntitiesCount];

            foreach (GameObject internObject in PluginRuntimeProvider.Context.InternObjects)
            {
                PlayerControllerB internController = internObject.GetComponentInChildren<PlayerControllerB>();
                // Radar
                StartOfRound.Instance.mapScreen.radarTargets.Add(new TransformAndName(internController.transform, internController.playerUsername, false));

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

                internObject.SetActive(false);
            }

            UpdateSoundManagerWithInterns(AllEntitiesCount);
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

        public void DestroyMonoManagers()
        {
            Object.Destroy(AudioManager.Instance);
            Object.Destroy(IdentityManager.Instance);
            Object.Destroy(InputManager.Instance);
            Object.Destroy(TargetingManager.Instance);
            Object.Destroy(UIManager.Instance);
        }
    }
}
