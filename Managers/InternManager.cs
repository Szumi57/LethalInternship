using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Utils;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LethalInternship.Managers
{
    internal class NetworkPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        public uint Id;
        private GameObject _gameObject;
        public NetworkPrefabInstanceHandler(ulong playerId, GameObject gameObject)
        {
            _gameObject = gameObject;

            byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + gameObject.name + playerId));
            Id = BitConverter.ToUInt32(value, 0);
        }
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            Type type = typeof(NetworkObject);
            FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);

            var networkObject = _gameObject.GetComponent<NetworkObject>();
            fieldInfo.SetValue(networkObject, Id);
            return networkObject;
        }
        public void Destroy(NetworkObject networkObject) { }
    }

    internal class NoHashNetworkPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        public uint Id;
        private NetworkObject _gameObject;
        public NoHashNetworkPrefabInstanceHandler(NetworkObject gameObject)
        {
            _gameObject = gameObject;

            Type type = typeof(NetworkObject);
            FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);
            Id = (uint)fieldInfo.GetValue(gameObject);
        }
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var networkObject = _gameObject.GetComponent<NetworkObject>();
            return networkObject;
        }
        public void Destroy(NetworkObject networkObject) { }
    }

    internal class InternManager : NetworkBehaviour
    {
        public static InternManager Instance { get; private set; } = null!;

        public int AllEntitiesCount;
        public int NbInternsOwned;
        public int NbInternsToDropShip;
        public int IndexBeginOfInterns
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Length - AllInternAIs?.Length ?? 0;
            }
        }
        public int NbInternsPurchasable { get { return Const.INTERN_AVAILABLE_MAX - NbInternsOwned; } }

        private InternAI[] AllInternAIs = null!;
        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        private void Awake()
        {
            Instance = this;
            Plugin.Logger.LogDebug($"InternManager Awake !!!!");

            if (Plugin.IrlPlayersCount > 0)
            {
                // only resize if irl players not 0, which means we already tried to populate pool of interns
                // But the manager somehow reset
                ManagePoolOfInterns();
            }
        }

        public void ManagePoolOfInterns()
        {
            StartOfRound instance = StartOfRound.Instance;

            // Initialize back ups
            if (AllPlayerObjectsBackUp == null || AllPlayerObjectsBackUp.Length != Const.INTERN_AVAILABLE_MAX)
            {
                AllInternAIs = new InternAI[Const.INTERN_AVAILABLE_MAX];
                AllPlayerObjectsBackUp = new GameObject[Const.INTERN_AVAILABLE_MAX];
                AllPlayerScriptsBackUp = new PlayerControllerB[Const.INTERN_AVAILABLE_MAX];
            }

            if (instance.allPlayerScripts.Length == AllEntitiesCount)
            {
                // the arrays have not been resize between round
                return;
            }

            //PlayerControllerB player;
            //int indexFirstInternStored = instance.allPlayerScripts.Length;
            //for (int i = 0; i < instance.allPlayerScripts.Length; i++)
            //{
            //    player = instance.allPlayerScripts[i];
            //    if (!string.IsNullOrWhiteSpace(player.playerUsername)
            //        && player.playerUsername.StartsWith(Const.INTERN_NAME))
            //    {
            //        indexFirstInternStored = i;
            //        break;
            //    }
            //}
            int irlPlayersCount;
            if (Plugin.IrlPlayersCount == 0)
            {
                irlPlayersCount = instance.allPlayerScripts.Length;
            }
            else
            {
                irlPlayersCount = Plugin.IrlPlayersCount - Const.INTERN_AVAILABLE_MAX < 0 ? 0 : Plugin.IrlPlayersCount - Const.INTERN_AVAILABLE_MAX;
            }

            ResizePoolOfInterns(irlPlayersCount);
            PopulatePoolOfInterns(irlPlayersCount);
        }

        private void ResizePoolOfInterns(int irlPlayersCount)
        {
            StartOfRound instance = StartOfRound.Instance;
            Plugin.Logger.LogDebug($"instance.allPlayerObjects.Length {instance.allPlayerObjects.Length} AllEntitiesCount {AllEntitiesCount}");
            if (instance.allPlayerObjects.Length == AllEntitiesCount)
            {
                // the arrays have not been resize between round
                Plugin.Logger.LogDebug($"The arrays have not been resize between round {instance.allPlayerObjects.Length}, AllEntitiesCount {AllEntitiesCount}, AllInternAIs {AllInternAIs} {AllInternAIs?.Length}");

                //NetworkObject networkObject = internObject.GetComponent<NetworkObject>();
                //byte[] hashBytevalue = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + networkObject.name + indexPlusIrlPlayersCount));
                //uint hash = BitConverter.ToUInt32(hashBytevalue, 0);
                //fieldGlobalObjectIdHash.SetValue(networkObject, hash);

                //Scene scene = networkObject.gameObject.scene;
                //int handle = scene.handle;
                //uint keyHash = hash;
                //if (!scenePlacedObjects.ContainsKey(keyHash))
                //{
                //    scenePlacedObjects.Add(keyHash, new Dictionary<int, NetworkObject>());
                //}
                //if (scenePlacedObjects[keyHash].ContainsKey(handle))
                //{
                //    string arg = scenePlacedObjects[keyHash][handle] != null ? scenePlacedObjects[keyHash][handle].name : "Null Entry";
                //    throw new Exception(networkObject.name + " tried to registered with ScenePlacedObjects which already contains " + string.Format("the same {0} value {1} for {2}!", "GlobalObjectIdHash", keyHash, arg));
                //}
                //scenePlacedObjects[keyHash].Add(handle, networkObject);

                return;
            }

            int irlPlayersAndInternsCount = irlPlayersCount + Const.INTERN_AVAILABLE_MAX;
            Array.Resize(ref instance.allPlayerObjects, irlPlayersAndInternsCount);
            Array.Resize(ref instance.allPlayerScripts, irlPlayersAndInternsCount);
            Array.Resize(ref instance.gameStats.allPlayerStats, irlPlayersAndInternsCount);
            Array.Resize(ref instance.playerSpawnPositions, irlPlayersAndInternsCount);
            Plugin.Logger.LogDebug($"Resize for interns from irl players count of {irlPlayersCount} to {irlPlayersAndInternsCount}");

            AllEntitiesCount = irlPlayersAndInternsCount;
            Plugin.IrlPlayersCount = irlPlayersAndInternsCount;
        }

        private void PopulatePoolOfInterns(int irlPlayersCount)
        {
            StartOfRound instance = StartOfRound.Instance;
            GameObject internObjectParent = instance.allPlayerObjects[3].gameObject;
            for (int i = 0; i < AllPlayerObjectsBackUp.Length; i++)
            {
                int indexPlusIrlPlayersCount = i + irlPlayersCount;
                if (AllPlayerObjectsBackUp[i] != null)
                {
                    //Plugin.Logger.LogDebug($"backup playerClientId {AllPlayerScriptsBackUp[i].playerClientId}");
                    //var aab = AllPlayerObjectsBackUp[i].GetComponentsInChildren<NetworkObject>();
                    //foreach (NetworkObject a in aab)
                    //{
                    //    Plugin.Logger.LogDebug($"back up hash ? {a.gameObject} {a.gameObject.name} hash {a.PrefabIdHash} {fieldInfo.GetValue(a)}");
                    //}

                    Plugin.Logger.LogDebug($"use of backup : {AllPlayerScriptsBackUp[i].playerUsername}");
                    instance.allPlayerObjects[indexPlusIrlPlayersCount] = AllPlayerObjectsBackUp[i];
                    instance.allPlayerScripts[indexPlusIrlPlayersCount] = AllPlayerScriptsBackUp[i];
                    instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                    instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];
                    continue;
                }

                GameObject internObject = Object.Instantiate<GameObject>(internObjectParent, internObjectParent.transform.parent);

                //NetworkObject networkObject = internObject.GetComponent<NetworkObject>();
                //byte[] hashBytevalue = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + networkObject.name + indexPlusIrlPlayersCount));
                //uint hash = BitConverter.ToUInt32(hashBytevalue, 0);
                //fieldGlobalObjectIdHash.SetValue(networkObject, hash);

                //Scene scene = networkObject.gameObject.scene;
                //int handle = scene.handle;
                //uint keyHash = hash;
                //if (!scenePlacedObjects.ContainsKey(keyHash))
                //{
                //    scenePlacedObjects.Add(keyHash, new Dictionary<int, NetworkObject>());
                //}
                //if (scenePlacedObjects[keyHash].ContainsKey(handle))
                //{
                //    string arg = scenePlacedObjects[keyHash][handle] != null ? scenePlacedObjects[keyHash][handle].name : "Null Entry";
                //    throw new Exception(networkObject.name + " tried to registered with ScenePlacedObjects which already contains " + string.Format("the same {0} value {1} for {2}!", "GlobalObjectIdHash", keyHash, arg));
                //}
                //scenePlacedObjects[keyHash].Add(handle, networkObject);

                PlayerControllerB internController = internObject.GetComponentInChildren<PlayerControllerB>();
                //todo unique name and unique id
                internController.playerClientId = (ulong)(indexPlusIrlPlayersCount);
                internController.isPlayerDead = false;
                internController.isPlayerControlled = false;
                internController.transform.localScale = new Vector3(Const.SIZE_SCALE_INTERN, Const.SIZE_SCALE_INTERN, Const.SIZE_SCALE_INTERN);
                internController.actualClientId = internController.playerClientId;
                internController.playerUsername = $"{Const.INTERN_NAME}{internController.playerClientId - (ulong)irlPlayersCount}";

                instance.allPlayerObjects[indexPlusIrlPlayersCount] = internObject;
                instance.allPlayerScripts[indexPlusIrlPlayersCount] = internController;
                instance.gameStats.allPlayerStats[indexPlusIrlPlayersCount] = new PlayerStats();
                instance.playerSpawnPositions[indexPlusIrlPlayersCount] = instance.playerSpawnPositions[3];

                AllPlayerObjectsBackUp[i] = internObject;
                AllPlayerScriptsBackUp[i] = internController;

                //var handler = new NetworkPrefabInstanceHandler(internController.playerClientId, internObject);
                //Plugin.Logger.LogDebug($"AddHandler ? {NetworkManager.Singleton.PrefabHandler.AddHandler(handler.Id, handler)}");

                //var handler = new NoHashNetworkPrefabInstanceHandler(internObject.GetComponent<NetworkObject>());
                //Plugin.Logger.LogDebug($"AddHandler ? {NetworkManager.Singleton.PrefabHandler.AddHandler(handler.Id, handler)}");

                //Plugin.Logger.LogDebug($"playerClientId {internController.playerClientId}");
                //var aa = internObject.GetComponentsInChildren<NetworkObject>();
                //foreach (NetworkObject networkObject in aa)
                //{
                //    if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(networkObject.gameObject))
                //    {
                //        NetworkManager.NetworkConfig.ForceSamePrefabs = false;
                //        //byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + networkObject.name + internController.playerClientId));
                //        //fieldInfo.SetValue(networkObject, BitConverter.ToUInt32(value, 0));
                //        NetworkManager.Singleton.AddNetworkPrefab(networkObject.gameObject);
                //        NetworkManager.NetworkConfig.ForceSamePrefabs = true;
                //    }
                //    Plugin.Logger.LogDebug($"hash ? {networkObject.gameObject} {networkObject.gameObject.name} hash {networkObject.PrefabIdHash} {fieldInfo.GetValue(networkObject)}");
                //}

                internObject.SetActive(false);
            }

            //foreach(var a in NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks)
            //{
            //    Plugin.Logger.LogDebug($"hash ? {a.Key} {a.Value.Prefab.name}");
            //}

            //foreach (InternAI ai in InternAIs)
            //{
            //    ai.enabled = true;
            //    GameObject internNPC = ai.NpcController.Npc.transform.root.gameObject;

            //    var listNetworkObjects = internNPC.GetComponentsInChildren<NetworkObject>();
            //    NetworkObject networkObjectRoot = null!;
            //    foreach (var networkObject in listNetworkObjects)
            //    {
            //        if (networkObject.transform.parent == null)
            //        {
            //            networkObjectRoot = networkObject;
            //            continue;
            //        }

            //        networkObject.Despawn(true);
            //    }
            //    networkObjectRoot.Despawn(true);

            //    Object.DestroyImmediate(ai.NpcController.Npc.gameObject);
            //    //Object.DestroyImmediate(ai.gameObject);
            //    NetworkManager.Singleton.PrefabHandler.RemoveHandler(StartOfRound.Instance.allPlayerObjects[3].gameObject);
            //    Object.DestroyImmediate(internNPC);
            //}
        }

        #region Spawn Intern

        [ServerRpc(RequireOwnership = false)]
        public void SpawnInternServerRpc(Vector3 spawnPosition, float yRot, bool isOutside)
        {
            int indexNextPlayerObject = GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                Plugin.Logger.LogInfo($"No more intern available");
                return;
            }

            int indexNextIntern = indexNextPlayerObject - IndexBeginOfInterns;

            NetworkObjectReference networkObjectReferenceInternAI = SpawnOrUseInternAI(indexNextIntern);
            NetworkObjectReference networkObjectReferenceObjectParent = default; //SpawnObjectIntern(indexNextPlayerObject);
            SpawnInternClientRpc(networkObjectReferenceInternAI, networkObjectReferenceObjectParent,
                                 indexNextIntern, indexNextPlayerObject,
                                 spawnPosition, yRot, isOutside);
        }

        private NetworkObjectReference SpawnOrUseInternAI(int indexNextIntern)
        {
            InternAI internAI = AllInternAIs[indexNextIntern];
            if (internAI != null)
            {
                return AllInternAIs[indexNextIntern].NetworkObject;
            }

            GameObject internPrefab = Object.Instantiate<GameObject>(Plugin.InternNPCPrefab.enemyPrefab);
            AllInternAIs[indexNextIntern] = internPrefab.GetComponent<InternAI>();

            NetworkObject networkObject = internPrefab.GetComponentInChildren<NetworkObject>();
            networkObject.Spawn(true);

            return networkObject;
        }

        [ClientRpc]
        private void SpawnInternClientRpc(NetworkObjectReference networkObjectReferenceInternAI, NetworkObjectReference networkObjectReferenceObjectParent,
                                             int indexNextIntern, int indexNextPlayerObject,
                                             Vector3 spawnPosition, float yRot, bool isOutside)
        {
            Plugin.Logger.LogInfo($"Client receive NOR after spawned on server...");

            StartOfRound instance = StartOfRound.Instance;

            networkObjectReferenceInternAI.TryGet(out NetworkObject networkObjectInternAI);
            InternAI internAI = networkObjectInternAI.gameObject.GetComponent<InternAI>();
            AllInternAIs[indexNextIntern] = internAI;

            //networkObjectReferenceObjectParent.TryGet(out NetworkObject networkObjectObjectParent);
            //GameObject objectParent = networkObjectObjectParent.gameObject;
            //instance.allPlayerObjects[indexNextPlayerObject] = objectParent;
            //instance.allPlayerScripts[indexNextPlayerObject] = objectParent.GetComponent<PlayerControllerB>();

            //Type type = typeof(NetworkObject);
            //FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);
            //var aa = objectParent.GetComponentsInChildren<NetworkObject>();
            //foreach (NetworkObject a in aa)
            //{
            //    Plugin.Logger.LogDebug($"spawned hash ? {a.gameObject} {a.gameObject.name} hash {a.PrefabIdHash} {fieldInfo.GetValue(a)}");
            //}

            internAI.SetEnemyOutside(isOutside);
            InitInternSpawning(internAI, indexNextPlayerObject, spawnPosition, yRot, isOutside);
        }

        private NetworkObjectReference SpawnObjectIntern(int indexNextPlayerObject)
        {
            GameObject objectParent = StartOfRound.Instance.allPlayerObjects[indexNextPlayerObject];
            return SpawnNetworkObjectsOfGameObject(objectParent, NetworkManager.ServerClientId);
        }

        private NetworkObject SpawnNetworkObjectsOfGameObject(GameObject gameObject, ulong playerId)
        {
            gameObject.SetActive(true);
            var listNetworkObjects = gameObject.GetComponentsInChildren<NetworkObject>();
            NetworkObject networkObjectRoot = null!;
            List<Tuple<NetworkObject, Transform>> listTupleTransformParentChild = new List<Tuple<NetworkObject, Transform>>();

            Type type = typeof(NetworkObject);
            FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (NetworkObject networkObject in listNetworkObjects)
            {
                if (networkObject.transform.parent == null)
                {
                    networkObjectRoot = networkObject;
                    continue;
                }

                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(networkObject.gameObject);

                if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(networkObject.gameObject))
                {
                    //NetworkManager.NetworkConfig.ForceSamePrefabs = false;
                    //byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + networkObject.name + playerId));
                    //fieldInfo.SetValue(networkObject, BitConverter.ToUInt32(value, 0));
                    //NetworkManager.Singleton.AddNetworkPrefab(networkObject.gameObject);
                    //Plugin.Logger.LogDebug($"AddNetworkPrefab {networkObject.gameObject} {networkObject.gameObject.name} {networkObject.PrefabIdHash}");
                    //NetworkManager.NetworkConfig.ForceSamePrefabs = true;
                }

                listTupleTransformParentChild.Add(new Tuple<NetworkObject, Transform>(networkObject, networkObject.transform.parent));
                if (!networkObject.IsSpawned)
                {
                    networkObject.Spawn(true);
                }
                Plugin.Logger.LogDebug($"++ {networkObject.gameObject} {networkObject.gameObject.name} IsSpawned {networkObject.IsSpawned}");
                Plugin.Logger.LogDebug($"hash ? {networkObject.gameObject} {networkObject.gameObject.name} hash {networkObject.PrefabIdHash} {fieldInfo.GetValue(networkObject)}");
            }

            if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(networkObjectRoot.gameObject))
            {
                //NetworkManager.NetworkConfig.ForceSamePrefabs = false;
                //byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + networkObjectRoot.name + playerId));
                //fieldInfo.SetValue(networkObjectRoot, BitConverter.ToUInt32(value, 0));
                //NetworkManager.Singleton.AddNetworkPrefab(networkObjectRoot.gameObject);
                //Plugin.Logger.LogDebug($"root -- {networkObjectRoot.gameObject} {networkObjectRoot.gameObject.name} {networkObjectRoot.PrefabIdHash}");
                //NetworkManager.NetworkConfig.ForceSamePrefabs = true;
            }
            if (!networkObjectRoot.IsSpawned)
            {
                networkObjectRoot.Spawn(true);
            }
            Plugin.Logger.LogDebug($"root ++ {networkObjectRoot.gameObject} {networkObjectRoot.gameObject.name} IsSpawned {networkObjectRoot.IsSpawned}");

            // Reparent what has been lost with spawn
            foreach (Tuple<NetworkObject, Transform> tupleTransformParentChild in listTupleTransformParentChild)
            {
                NetworkObject networkObject = tupleTransformParentChild.Item1;
                networkObject.enabled = false;
                networkObject.AutoObjectParentSync = false;

                networkObject.transform.parent = tupleTransformParentChild.Item2;

                networkObject.enabled = true;
                networkObject.AutoObjectParentSync = true;
            }

            //Plugin.Logger.LogDebug($"581939109 {PropertiesAndFieldsUtils.GetNetworkObjectByHash(581939109)}");
            //Plugin.Logger.LogDebug($"2946656848 {PropertiesAndFieldsUtils.GetNetworkObjectByHash(2946656848)}");
            //Plugin.Logger.LogDebug($"1429679652 {PropertiesAndFieldsUtils.GetNetworkObjectByHash(1429679652)}");
            //Plugin.Logger.LogDebug($"4114126056 {PropertiesAndFieldsUtils.GetNetworkObjectByHash(4114126056)}");
            //Plugin.Logger.LogDebug($"1493017890 {PropertiesAndFieldsUtils.GetNetworkObjectByHash(1493017890)}");

            //Player(3)(Clone)(UnityEngine.GameObject) Player(3)(Clone) hash 1493017890
            //ServerItemHolder(UnityEngine.GameObject) ServerItemHolder hash 2946656848
            //LocalItemHolder(UnityEngine.GameObject) LocalItemHolder hash 1429679652
            //PlayerPhysicsBox(UnityEngine.GameObject) PlayerPhysicsBox hash 4114126056

            return networkObjectRoot;
        }

        private void InitInternSpawning(InternAI internAI, int indexNextPlayerObject, Vector3 spawnPosition, float yRot, bool isOutside)
        {
            Plugin.Logger.LogDebug($"InitIntern AllEntitiesCount {AllEntitiesCount}, AllInternAIs {AllInternAIs} {AllInternAIs?.Length}");

            StartOfRound instance = StartOfRound.Instance;
            Plugin.Logger.LogDebug($"position : {spawnPosition}, yRot: {yRot}");
            GameObject objectParent = instance.allPlayerObjects[indexNextPlayerObject];
            objectParent.transform.position = spawnPosition;
            objectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, yRot, 0f));

            PlayerControllerB internController = instance.allPlayerScripts[indexNextPlayerObject];
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.health = Const.INTERN_MAX_HEALTH;
            internController.DisablePlayerModel(objectParent, true, true);
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
            internController.inAnimationWithEnemy = null;
            internController.holdingWalkieTalkie = false;
            internController.speakingToWalkieTalkie = false;
            internController.isSinking = false;
            internController.isUnderwater = false;
            internController.sinkingValue = 0f;
            internController.sourcesCausingSinking = 0;
            internController.isClimbingLadder = false;
            internController.disableLookInput = true;
            internController.setPositionOfDeadPlayer = false;
            internController.mapRadarDotAnimator.SetBool(Const.MAPDOT_ANIMATION_BOOL_DEAD, false);
            internController.externalForceAutoFade = Vector3.zero;
            internController.voiceMuffledByEnemy = false;
            internController.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, false);
            AccessTools.Field(typeof(PlayerControllerB), "updatePositionForNewlyJoinedClient").SetValue(internController, true);

            internAI.InternId = Array.IndexOf(AllInternAIs, internAI).ToString();
            Plugin.Logger.LogDebug($"Adding AI \"{internAI.InternId}\" for body {internController.playerUsername}");
            internAI.creatureAnimator = internController.playerBodyAnimator;
            internAI.NpcController = new NpcController(internController);
            internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");

            // Plug ai on intern body
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

        #endregion

        #region SpawnInternsFromDropShip

        public void SpawnInternsFromDropShip(Transform[] spawnPositions)
        {
            int pos = 0;
            for (int i = 0; i < NbInternsToDropShip; i++)
            {
                if (pos >= 3)
                {
                    pos = 0;
                }
                Transform transform = spawnPositions[pos++];
                SpawnInternServerRpc(transform.position, transform.eulerAngles.y, true);
            }
            EndSpawnInternsFromDropShipServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void EndSpawnInternsFromDropShipServerRpc()
        {
            EndSpawnInternsFromDropShipClientRpc();
        }

        [ClientRpc]
        private void EndSpawnInternsFromDropShipClientRpc()
        {
            NbInternsToDropShip = 0;
        }

        #endregion

        public bool AreInternsScheduledToLand()
        {
            // no drop of interns on company building moon
            if (StartOfRound.Instance.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID)
            {
                return false;
            }

            return NbInternsToDropShip > 0;
        }

        public bool IsObjectHeldByIntern(GrabbableObject grabbableObject)
        {
            Transform localItemHolder = grabbableObject.parentObject;
            PlayerControllerB playerHolder;

            if (localItemHolder == null)
            {
                playerHolder = grabbableObject.playerHeldBy;
            }
            else
            {
                playerHolder = localItemHolder.GetComponentInParent<PlayerControllerB>();
            }

            if (playerHolder == null) { return false; }
            return GetInternAI((int)playerHolder.playerClientId) != null;
        }

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

        [ServerRpc]
        public void SyncEndOfRoundInternsFromClientToServerRpc()
        {
            Plugin.Logger.LogInfo($"Client send to server to sync end of round, calling ClientRpc...");
            SyncEndOfRoundInternsFromServerToClientRpc();
        }

        [ClientRpc]
        public void SyncEndOfRoundInternsFromServerToClientRpc()
        {
            Plugin.Logger.LogInfo($"Server send to clients to sync end of round, client execute...");
            NbInternsOwned = CountAliveAndDisableInterns();
            NbInternsToDropShip = NbInternsOwned;
        }

        public void AddNewCommandOfInterns(int nbOrdered)
        {
            if (StartOfRound.Instance.inShipPhase)
            {
                // in space
                NbInternsOwned += nbOrdered;
                NbInternsToDropShip = NbInternsOwned;
                Plugin.Logger.LogDebug($"In space NbInternsOwned {NbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip}");
            }
            else
            {
                // on moon
                NbInternsToDropShip += nbOrdered;
                NbInternsOwned += nbOrdered;
                Plugin.Logger.LogDebug($"On moon NbInternsOwned {NbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip}");
            }
        }

        public void UpdateInternsOrdered(int nbInternsOwned, int nbInternToDropShip)
        {
            NbInternsOwned = nbInternsOwned;
            NbInternsToDropShip = nbInternToDropShip;
        }

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
            return alive;
        }

        private int GetNextAvailablePlayerObject()
        {
            StartOfRound instance = StartOfRound.Instance;
            Plugin.Logger.LogDebug($"IndexBeginOfInterns {IndexBeginOfInterns} instance.allPlayerScripts.Length {instance.allPlayerScripts.Length}");
            for (int i = IndexBeginOfInterns; i < instance.allPlayerScripts.Length; i++)
            {
                if (!instance.allPlayerScripts[i].isPlayerControlled)
                {
                    return i;
                }
            }
            return -1;
        }

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

        public InternAI? GetInternAiObjectOwnerOf(GrabbableObject grabbableObject)
        {
            foreach (var internAI in AllInternAIs)
            {
                if (!internAI.IsSpawned
                    || internAI.isEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.HeldItem == null)
                {
                    continue;
                }

                if (internAI.HeldItem.NetworkObjectId == grabbableObject.NetworkObjectId)
                {
                    return internAI;
                }
            }

            return null;
        }

        public bool IsIdPlayerIntern(int id)
        {
            return id >= IndexBeginOfInterns;
        }

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

        public Vector3 ShipBoundClosestPoint(Vector3 fromPoint)
        {
            return GetExpandedShipBounds().ClosestPoint(fromPoint);
        }

        public Bounds GetExpandedShipBounds()
        {
            Bounds shipBounds = new Bounds(StartOfRound.Instance.shipBounds.bounds.center, StartOfRound.Instance.shipBounds.bounds.size);
            shipBounds.Expand(6f);
            return shipBounds;
        }

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

        public bool IsColliderFromLocalOrInternOwnerLocal(Collider collider)
        {
            PlayerControllerB player = collider.gameObject.GetComponent<PlayerControllerB>();
            return IsPlayerLocalOrInternOwnerLocal(player);
        }

        public bool IsPlayerIntern(PlayerControllerB player)
        {
            if (player == null) return false;
            InternAI? internAI = GetInternAI((int)player.playerClientId);
            return internAI != null;
        }

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

        public bool IsIdPlayerInternOwnerLocal(int idPlayer)
        {
            InternAI? internAI = GetInternAI(idPlayer);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }
    }
}
