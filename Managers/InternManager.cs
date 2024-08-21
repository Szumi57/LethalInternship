using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Configs;
using LethalInternship.Enums;
using LethalInternship.Patches.NpcPatches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LethalInternship.Managers
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
    internal class InternManager : NetworkBehaviour
    {
        public static InternManager Instance { get; private set; } = null!;

        /// <summary>
        /// Size of allPlayerScripts, AllPlayerObjects, for normal players controller + interns player controllers
        /// </summary>
        public int AllEntitiesCount;
        /// <summary>
        /// Number of interns already bought
        /// </summary>
        public int NbInternsOwned;
        /// <summary>
        /// Number of interns, among those already bought, scheduled to dropship on moon
        /// </summary>
        public int NbInternsToDropShip;
        /// <summary>
        /// Integer corresponding to the first player controller associated with an intern in StartOfRound.Instance.allPlayerScripts
        /// </summary>
        public int IndexBeginOfInterns
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Length - AllInternAIs?.Length ?? 0;
            }
        }
        /// <summary>
        /// Maximum interns actually purchasable minus the ones already bought
        /// </summary>
        public int NbInternsPurchasable
        {
            get
            {
                int val = Plugin.Config.MaxInternsAvailable.Value - NbInternsOwned;
                return val < 0 ? 0 : val;
            }
        }
        public bool LandingStatusAllowed;

        public VehicleController? VehicleController;

        private InternAI[] AllInternAIs = null!;
        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        private Bounds? shipBoundsExpanded;

        private List<string> listOfNames = new List<string>();
        private EnumOptionInternNames optionInternNames;
        private string[] arrayOfUserCustomNames = null!;
        private int indexIterationName = 0;

        /// <summary>
        /// Initialize instance,
        /// repopulate pool of interns if InternManager reset when loading game
        /// </summary>
        private void Awake()
        {
            Instance = this;
            Plugin.Config.InitialSyncCompleted += Config_InitialSyncCompleted;
            Plugin.LogDebug($"Client {NetworkManager.LocalClientId}, MaxInternsAvailable before CSync {Plugin.Config.MaxInternsAvailable.Value}");

            if (Plugin.PluginIrlPlayersCount > 0)
            {
                // only resize if irl players not 0, which means we already tried to populate pool of interns
                // But the manager somehow reset
                ManagePoolOfInterns();
            }
        }

        private void Config_InitialSyncCompleted(object sender, EventArgs e)
        {
            if (IsHost)
            {
                return;
            }

            Plugin.LogDebug($"Client {NetworkManager.LocalClientId}, ManagePoolOfInterns after CSync, MaxInternsAvailable {Plugin.Config.MaxInternsAvailable.Value}");
            ManagePoolOfInterns();
        }

        /// <summary>
        /// Initialize, resize and populate allPlayerScripts, allPlayerObjects with new interns
        /// </summary>
        public void ManagePoolOfInterns()
        {
            StartOfRound instance = StartOfRound.Instance;
            int maxInternsAvailable = Plugin.Config.MaxInternsAvailable.Value;

            if (instance.allPlayerObjects[3].gameObject == null)
            {
                Plugin.LogInfo("No player objects initialized in game, aborting interns initializations.");
                return;
            }

            // Initialize back ups
            if (AllPlayerObjectsBackUp == null)
            {
                AllInternAIs = new InternAI[maxInternsAvailable];
                AllPlayerObjectsBackUp = new GameObject[maxInternsAvailable];
                AllPlayerScriptsBackUp = new PlayerControllerB[maxInternsAvailable];
            }
            else if (AllPlayerObjectsBackUp.Length != maxInternsAvailable)
            {
                Array.Resize(ref AllInternAIs, maxInternsAvailable);
                Array.Resize(ref AllPlayerObjectsBackUp, maxInternsAvailable);
                Array.Resize(ref AllPlayerScriptsBackUp, maxInternsAvailable);
            }

            if (Plugin.PluginIrlPlayersCount == 0)
            {
                Plugin.PluginIrlPlayersCount = instance.allPlayerObjects.Length;
                Plugin.LogDebug($"PluginIrlPlayersCount = {Plugin.PluginIrlPlayersCount}");
            }
            int irlPlayersCount = Plugin.PluginIrlPlayersCount;
            int irlPlayersAndInternsCount = irlPlayersCount + maxInternsAvailable;
            AllEntitiesCount = irlPlayersAndInternsCount;

            if (instance.allPlayerScripts.Length == AllEntitiesCount)
            {
                // the arrays have not been resize between round
                Plugin.LogInfo("Pool of interns ok. The arrays have not been resize");
                return;
            }

            ResizePoolOfInterns(irlPlayersAndInternsCount);
            PopulatePoolOfInterns(irlPlayersCount);
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
            Plugin.LogDebug($"Resized arrays from {previousSize} to {irlPlayersAndInternsCount}");
        }

        /// <summary>
        /// Populate allPlayerScripts, allPlayerObjects with new controllers, instantiated of the 4th player, initiated and named
        /// </summary>
        /// <param name="irlPlayersCount">Number of "real" players, 4 base game (without morecompany), for calculating parameterization</param>
        private void PopulatePoolOfInterns(int irlPlayersCount)
        {
            Plugin.LogDebug($"Attempt to populate pool of interns. irlPlayersCount {irlPlayersCount}");
            StartOfRound instance = StartOfRound.Instance;
            GameObject internObjectParent = instance.allPlayerObjects[3].gameObject;

            optionInternNames = Plugin.Config.GetOptionInternNames();
            arrayOfUserCustomNames = Plugin.Config.GetArrayOfUserCustomNames();
            indexIterationName = 0;

            // Using back up if available,
            // If the size of array has been modified by morecompany for example when loading scene or the game, at some point
            for (int i = 0; i < AllPlayerObjectsBackUp.Length; i++)
            {
                int indexPlusIrlPlayersCount = i + irlPlayersCount;
                if (AllPlayerObjectsBackUp[i] != null)
                {
                    Plugin.LogDebug($"PopulatePoolOfInterns - use of backup : {AllPlayerScriptsBackUp[i].playerUsername}");
                    instance.allPlayerObjects[indexPlusIrlPlayersCount] = AllPlayerObjectsBackUp[i];
                    instance.allPlayerScripts[indexPlusIrlPlayersCount] = AllPlayerScriptsBackUp[i];
                    instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                    instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];
                    continue;
                }

                GameObject internObject = Object.Instantiate<GameObject>(internObjectParent, internObjectParent.transform.parent);

                PlayerControllerB internController = internObject.GetComponentInChildren<PlayerControllerB>();
                //todo unique name and unique id
                internController.playerClientId = (ulong)(indexPlusIrlPlayersCount);
                internController.isPlayerDead = false;
                internController.isPlayerControlled = false;
                internController.transform.localScale = new Vector3(Plugin.Config.InternSizeScale.Value, Plugin.Config.InternSizeScale.Value, Plugin.Config.InternSizeScale.Value);
                internController.thisController.radius *= Plugin.Config.InternSizeScale.Value;
                internController.actualClientId = internController.playerClientId;
                internController.playerUsername = string.Format(Const.DEFAULT_INTERN_NAME, internController.playerClientId - (ulong)irlPlayersCount);

                instance.allPlayerObjects[indexPlusIrlPlayersCount] = internObject;
                instance.allPlayerScripts[indexPlusIrlPlayersCount] = internController;
                instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];

                AllPlayerObjectsBackUp[i] = internObject;
                AllPlayerScriptsBackUp[i] = internController;

                internObject.SetActive(false);
            }

            Plugin.LogInfo("Pool of interns populated.");
        }

        #region Spawn Intern

        /// <summary>
        /// Rpc method on server spawning network object from intern prefab and calling the client
        /// </summary>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnInternServerRpc(Vector3 spawnPosition, float yRot, bool isOutside)
        {
            int indexNextPlayerObject = GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                Plugin.LogInfo($"No more intern available");
                return;
            }

            NetworkObject networkObject;
            int indexNextIntern = indexNextPlayerObject - IndexBeginOfInterns;
            InternAI internAI = AllInternAIs[indexNextIntern];
            if (internAI != null)
            {
                // Use internAI if exists
                networkObject = AllInternAIs[indexNextIntern].NetworkObject;
            }
            else
            {
                // Or spawn one (server only)
                GameObject internPrefab = Object.Instantiate<GameObject>(Plugin.InternNPCPrefab.enemyPrefab);
                internAI = internPrefab.GetComponent<InternAI>();
                AllInternAIs[indexNextIntern] = internAI;

                networkObject = internPrefab.GetComponentInChildren<NetworkObject>();
                networkObject.Spawn(true);
            }

            // Get a name for the intern
            string internName = StartOfRound.Instance.allPlayerScripts[indexNextPlayerObject].playerUsername;
            if (!internAI.AlreadyNamed)
            {
                internName = GetNameForIntern(internName, optionInternNames, arrayOfUserCustomNames);
                internAI.AlreadyNamed = true;
            }

            // Send to client to spawn intern
            SpawnInternClientRpc(networkObject,
                                 indexNextIntern, indexNextPlayerObject,
                                 internName,
                                 spawnPosition, yRot, isOutside);
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

        private string GetNameForIntern(string defaultName, EnumOptionInternNames optionInternNames, string[] arrayOfUserCustomNames)
        {
            switch (optionInternNames)
            {
                case EnumOptionInternNames.Default:
                    return defaultName;

                case EnumOptionInternNames.DefaultCustomList:
                    return GetRandomNameFromArray(Const.DEFAULT_LIST_CUSTOM_INTERN_NAMES);

                case EnumOptionInternNames.UserCustomList:
                    return GetRandomNameFromArray(arrayOfUserCustomNames);

                default:
                    Plugin.LogWarning($"Option for intern names invalid: {optionInternNames}");
                    return defaultName;
            }
        }

        private string GetRandomNameFromArray(string[] originalArrayOfNames)
        {
            if (listOfNames.Count == 0)
            {
                listOfNames.AddRange(originalArrayOfNames);
            }

            if (listOfNames.Count == 0)
            {
                return "List of names empty !";
            }

            string name;
            string iterationString = indexIterationName == 0 ? string.Empty : $" ({indexIterationName})";

            if (listOfNames.Count == 1)
            {
                name = listOfNames[0] + iterationString;
                listOfNames.RemoveAt(0);
                listOfNames.AddRange(originalArrayOfNames);
                indexIterationName++;
                return name;
            }

            int index;
            if (Plugin.Config.UseCustomNamesRandomly.Value)
            {
                Random randomInstance = new Random();
                index = randomInstance.Next(0, listOfNames.Count - 1);
            }
            else
            {
                index = 0;
            }

            name = listOfNames[index] + iterationString;
            listOfNames.RemoveAt(index);
            return name;
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
                                          int indexNextIntern, int indexNextPlayerObject,
                                          string internName,
                                          Vector3 spawnPosition, float yRot, bool isOutside)
        {
            Plugin.LogInfo($"Client receive RPC to spawn intern... position : {spawnPosition}, yRot: {yRot}");

            networkObjectReferenceInternAI.TryGet(out NetworkObject networkObjectInternAI);
            InternAI internAI = networkObjectInternAI.gameObject.GetComponent<InternAI>();
            AllInternAIs[indexNextIntern] = internAI;

            internAI.SetEnemyOutside(isOutside);
            InitInternSpawning(internAI, indexNextPlayerObject, internName, spawnPosition, yRot, isOutside);
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
        private void InitInternSpawning(InternAI internAI, int indexNextPlayerObject,
                                        string internName,
                                        Vector3 spawnPosition, float yRot, bool isOutside)
        {
            StartOfRound instance = StartOfRound.Instance;
            GameObject objectParent = instance.allPlayerObjects[indexNextPlayerObject];
            objectParent.transform.position = spawnPosition;
            objectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, yRot, 0f));

            PlayerControllerB internController = instance.allPlayerScripts[indexNextPlayerObject];
            internController.playerUsername = internName;
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.playerActions = new PlayerActions();
            internController.health = Plugin.Config.InternMaxHealth.Value;
            DisableInternControllerModel(objectParent, internController, true, true);
            internController.isInsideFactory = !isOutside;
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
            AccessTools.Field(typeof(PlayerControllerB), "updatePositionForNewlyJoinedClient").SetValue(internController, true);

            internAI.InternId = Array.IndexOf(AllInternAIs, internAI).ToString();
            internAI.creatureAnimator = internController.playerBodyAnimator;
            internAI.NpcController = new NpcController(internController);
            internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");

            // Plug ai on intern body
            Plugin.LogDebug($"Adding AI \"{internAI.InternId}\" for body {internController.playerUsername}");
            internAI.enabled = false;
            internAI.NetworkObject.AutoObjectParentSync = false;
            internAI.transform.parent = objectParent.transform;
            internAI.NetworkObject.AutoObjectParentSync = true;
            internAI.enabled = true;

            objectParent.SetActive(true);

            // Unsuscribe from events to prevent double trigger
            PlayerControllerBPatch.OnDisable_ReversePatch(internController);

            internAI.Init();
        }

        /// <summary>
        /// Manual DisablePlayerModel, for compatibility with mod LethalPhones, does not trigger patch of DisablePlayerModel in LethalPhones
        /// </summary>
        private void DisableInternControllerModel(GameObject internObject, PlayerControllerB internController, bool enable = false, bool disableLocalArms = false)
        {
            SkinnedMeshRenderer[] componentsInChildren = internObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = enable;
            }
            if (disableLocalArms)
            {
                internController.thisPlayerModelArms.enabled = false;
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
            for (int i = 0; i < NbInternsToDropShip; i++)
            {
                if (pos >= 3)
                {
                    pos = 0;
                }
                Transform transform = spawnPositions[pos++];
                SpawnInternServerRpc(transform.position, transform.eulerAngles.y, true);
                yield return new WaitForSeconds(0.1f);
            }
            EndSpawnInternsFromDropShipServerRpc();
        }

        /// <summary>
        /// Server side, call clients for indicating that all interns landed and no more are to drop
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void EndSpawnInternsFromDropShipServerRpc()
        {
            EndSpawnInternsFromDropShipClientRpc();
        }

        /// <summary>
        /// Client side, indicating that all interns landed and no more are to drop
        /// </summary>
        [ClientRpc]
        private void EndSpawnInternsFromDropShipClientRpc()
        {
            NbInternsToDropShip = 0;
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

            return NbInternsToDropShip > 0 && LandingStatusAllowed;
        }

        /// <summary>
        /// Adds, while in space or on moon, the number of intern bought and ready to drop ship
        /// </summary>
        /// <remarks>Used on client side for quick update of terminal infos</remarks>
        /// <param name="nbOrdered">Number of intern just ordered</param>
        public void AddNewCommandOfInterns(int nbOrdered)
        {
            if (StartOfRound.Instance.inShipPhase)
            {
                // in space
                NbInternsOwned += nbOrdered;
                NbInternsToDropShip = NbInternsOwned;
                Plugin.LogDebug($"In space NbInternsOwned {NbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip}");
            }
            else
            {
                // on moon
                NbInternsToDropShip += nbOrdered;
                NbInternsOwned += nbOrdered;
                Plugin.LogDebug($"On moon NbInternsOwned {NbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip}");
            }
        }

        /// <summary>
        /// Update, while in space or on moon, the number of intern bought and ready to drop ship
        /// </summary>
        /// <remarks>Used when syncing (server, client) numbers of interns bought and to dropship</remarks>
        /// <param name="nbInternsOwned">Number of interns bought</param>
        /// <param name="nbInternToDropShip">Number of interns to dropship on next moon</param>
        public void UpdateInternsOrdered(int nbInternsOwned, int nbInternToDropShip)
        {
            NbInternsOwned = nbInternsOwned;
            NbInternsToDropShip = nbInternToDropShip;
        }

        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB</c> <c>playerClientId</c>
        /// </summary>
        /// <param name="index"><c>playerClientId</c> of <c>PlayerControllerB</c></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated, else returns null</returns>
        public InternAI? GetInternAI(int index)
        {
            if (AllInternAIs == null)
            {
                // Ai not yet initialized
                return null;
            }

            int oldPlayersCount = StartOfRound.Instance.allPlayerObjects.Length - AllInternAIs.Length;
            if (index < IndexBeginOfInterns)
            {
                return null;
            }

            if (AllInternAIs.Length > 0)
            {
                return AllInternAIs[index < 0 ? 0 : index - oldPlayersCount];
            }
            return null;
        }

        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB.playerClientId</c>, 
        /// only if the local client calling the method is the owner of <c>InternAI</c>
        /// </summary>
        /// <param name="index"></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated and the local client is the owner, 
        /// else returns null</returns>
        public InternAI? GetInternAIIfLocalIsOwner(int index)
        {
            InternAI? internAI = GetInternAI(index);
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
        public InternAI? GetInternAiObjectOwnerOf(GrabbableObject grabbableObject)
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.isEnemyDead
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
            InternAI? internAI = GetInternAI((int)player.playerClientId);
            return internAI != null;
        }

        /// <summary>
        /// Is the <c>EnemyAI</c> is an <c>InternAI</c>
        /// </summary>
        /// <param name="ai"></param>
        /// <returns><c>true</c> if <c>EnemyAI</c> is <c>InternAI</c>, else <c>false</c></returns>
        public bool IsAIInternAi(EnemyAI ai)
        {
            for (int i = 0; i < AllInternAIs.Length; i++)
            {
                if (AllInternAIs[i] == ai)
                {
                    return true;
                }
            }

            return false;
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

            InternAI? internAI = GetInternAI((int)player.playerClientId);
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

            InternAI? internAI = GetInternAI((int)player.playerClientId);
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
            InternAI? internAI = GetInternAI(idPlayer);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        public InternAI[] GetInternsAIOwnedByLocal()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            List<InternAI> results = new List<InternAI>();
            InternAI? internAI;
            for (int i = IndexBeginOfInterns; i < instanceSOR.allPlayerScripts.Length; i++)
            {
                internAI = GetInternAI((int)instanceSOR.allPlayerScripts[i].playerClientId);
                if (internAI != null
                    && internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
                {
                    results.Add(internAI);
                }
            }
            return results.ToArray();
        }

        /// <summary>
        /// Get the closest point on the ship expanded (by <c>GetExpandedShipBounds()</c>) bounds from <c>fromPoint</c>
        /// </summary>
        /// <param name="fromPoint">Position of reference</param>
        public Vector3 ShipBoundClosestPoint(Vector3 fromPoint)
        {
            return GetExpandedShipBounds().ClosestPoint(fromPoint);
        }

        /// <summary>
        /// Get the expanded bounds of the ship by <c>Const.SHIP_EXPANDING_BOUNDS_DIFFERENCE</c> as a struct <c>Bounds</c>
        /// </summary>
        /// <returns></returns>
        public Bounds GetExpandedShipBounds()
        {
            if (shipBoundsExpanded == null)
            {
                Bounds shipBounds = new Bounds(StartOfRound.Instance.shipBounds.bounds.center, StartOfRound.Instance.shipBounds.bounds.size);
                shipBounds.Expand(Const.SHIP_EXPANDING_BOUNDS_DIFFERENCE);
                shipBoundsExpanded = shipBounds;
            }

            return shipBoundsExpanded.Value;
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
            NbInternsOwned = CountAliveAndDisableInterns();
            NbInternsToDropShip = NbInternsOwned;
        }

        /// <summary>
        /// Count and disable the interns still alive
        /// </summary>
        /// <returns>Number of interns still alive</returns>
        private int CountAliveAndDisableInterns()
        {
            StartOfRound instance = StartOfRound.Instance;
            if (instance.currentLevel.levelID == 3)
            {
                return NbInternsOwned;
            }

            int alive = 0;
            for (int i = IndexBeginOfInterns; i < instance.allPlayerScripts.Length; i++)
            {
                if (!instance.allPlayerScripts[i].isPlayerDead && instance.allPlayerScripts[i].isPlayerControlled)
                {
                    instance.allPlayerScripts[i].isPlayerControlled = false;
                    instance.allPlayerObjects[i].SetActive(false);
                    alive++;
                }
            }

            // Alive and not landed interns
            return alive + NbInternsToDropShip;
        }

        #endregion

        #region Vehicle landing on map RPC

        /// <summary>
        /// Update the stopping the perfoming of emote
        /// </summary>
        [ClientRpc]
        public void VehicleHasLandedClientRpc()
        {
            VehicleController = Object.FindObjectOfType<VehicleController>();
            Plugin.LogDebug($"Vehicle has landed : {VehicleController}");
        }

        #endregion
    }
}
