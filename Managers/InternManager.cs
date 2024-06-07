using GameNetcodeStuff;
using LethalInternship.AI;
using LethalInternship.Patches.NpcPatches;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Managers
{
    internal class InternManager : NetworkBehaviour
    {
        public static InternManager Instance { get; private set; } = null!;

        public int AllEntitiesCount;
        public InternAI[] AllInternAIs = null!;
        public EnemyType InternNPCPrefab = null!;

        public int NbInternsOwned;
        public int NbInternsToDropShip;

        public int IndexBeginOfInterns { get { return StartOfRound.Instance.allPlayerScripts.Length - AllInternAIs.Length; } }
        public int NbInternsPurchasable { get { return Const.INTERN_AVAILABLE_MAX - NbInternsOwned; } }

        private GameObject[] AllPlayerObjectsBackUp = null!;
        private PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        private void Awake()
        {
            Instance = this;
            Init();
        }

        private void Init()
        {
            //
            InternNPCPrefab = Plugin.ModAssets.LoadAsset<EnemyType>("InternNPC");
            if (InternNPCPrefab != null)
            {
                foreach (var transform in InternNPCPrefab.enemyPrefab.GetComponentsInChildren<Transform>()
                                                                   .Where(x => x.parent != null && x.parent.name == "InternNPCObj"
                                                                                                //&& x.name != "ScanNode"
                                                                                                && x.name != "MapDot"
                                                                                                //&& x.name != "Collision"
                                                                                                && x.name != "TurnCompass"
                                                                                                && x.name != "CreatureSFX"
                                                                                                //&& x.name != "CreatureVoice"
                                                                                                )
                                                                   .ToList())
                {
                    Object.DestroyImmediate(transform.gameObject);
                }
            }
        }

        public bool AreInternsScheduledToLand()
        {
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
            if (base.IsOwner)
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

        public void NewCommandOfInterns(int nbOrdered)
        {
            if (StartOfRound.Instance.inShipPhase)
            {
                // in space
                NbInternsOwned += nbOrdered;
                NbInternsToDropShip = NbInternsOwned;
            }
            else
            {
                // on moon
                NbInternsToDropShip = nbOrdered;
                NbInternsOwned += NbInternsToDropShip;
            }
        }

        private int CountAliveAndDisableInterns()
        {
            StartOfRound instance = StartOfRound.Instance;
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

        public void SpawnInternsFromDropShip(Transform[] spawnPositions)
        {
            int pos = 0;
            for (int i = 0; i < NbInternsToDropShip; i++)
            {
                if (pos >= 3)
                {
                    pos = 0;
                }
                SpawnIntern(spawnPositions[pos++]);
            }
            NbInternsToDropShip = 0;
        }

        public void SpawnIntern(Transform positionTransform, bool isOutside = true)
        {
            StartOfRound instance = StartOfRound.Instance;
            int indexNextPlayerObject = GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                Plugin.Logger.LogInfo($"No more intern available");
                return;
            }
            Plugin.Logger.LogDebug($"indexNextPlayerObject {indexNextPlayerObject}");

            Vector3 spawnPosition = positionTransform.position;
            float yRot = positionTransform.eulerAngles.y;
            Plugin.Logger.LogDebug($"position : {spawnPosition}, yRot: {yRot}");

            GameObject internObjectParent = instance.allPlayerObjects[indexNextPlayerObject];
            internObjectParent.transform.position = spawnPosition;
            internObjectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, yRot, 0f));

            PlayerControllerB internController = instance.allPlayerScripts[indexNextPlayerObject];
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.health = 50;
            internController.DisablePlayerModel(internObjectParent, true, true);
            internController.isInsideFactory = !isOutside;
            internController.isMovementHindered = 0;
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

            int indexNextIntern = indexNextPlayerObject - (instance.allPlayerScripts.Length - AllInternAIs.Length);
            Plugin.Logger.LogDebug($"Adding AI for intern {indexNextIntern} for body {internController.playerUsername}");
            InternAI internAI = null!;
            internAI = AllInternAIs[indexNextIntern];
            if (internAI == null)
            {
                GameObject internPrefab = Object.Instantiate<GameObject>(InternNPCPrefab.enemyPrefab);
                internPrefab.GetComponentInChildren<NetworkObject>().Spawn(true);
                internAI = internPrefab.GetComponent<InternAI>();
                internAI.creatureAnimator = internController.playerBodyAnimator;
                internAI.NpcController = new NpcController(internController);
                internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");


                internAI.LineRenderer1 = new GameObject().AddComponent<LineRenderer>();
                internAI.LineRenderer1.gameObject.transform.SetParent(internAI.transform, false);
                internAI.LineRenderer1.gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                internAI.LineRenderer2 = new GameObject().AddComponent<LineRenderer>();
                internAI.LineRenderer2.gameObject.transform.SetParent(internAI.transform, false);
                internAI.LineRenderer2.gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                internAI.LineRenderer3 = new GameObject().AddComponent<LineRenderer>();
                internAI.LineRenderer3.gameObject.transform.SetParent(internAI.transform, false);
                internAI.LineRenderer3.gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                internAI.LineRenderer4 = new GameObject().AddComponent<LineRenderer>();
                internAI.LineRenderer4.gameObject.transform.SetParent(internAI.transform, false);
                internAI.LineRenderer4.gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                internAI.LineRenderer5 = new GameObject().AddComponent<LineRenderer>();
                internAI.LineRenderer5.gameObject.transform.SetParent(internAI.transform, false);
                internAI.LineRenderer5.gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                AllInternAIs[indexNextIntern] = internAI;
            }

            // Plug ai on intern body
            internAI.transform.parent = internObjectParent.transform;

            internAI.SetEnemyOutside(isOutside);
            internAI.Init();

            internObjectParent.SetActive(true);

            // Unsuscribe from events to prevent double trigger
            PlayerControllerBPatch.OnDisable_ReversePatch(internController);
        }

        private int GetNextAvailablePlayerObject()
        {
            StartOfRound instance = StartOfRound.Instance;
            //Plugin.Logger.LogDebug($"2 instance.allPlayerScripts.Length : {instance.allPlayerScripts.Length}");
            //Plugin.Logger.LogDebug($"2 instance.allPlayerObjects.Length : {instance.allPlayerScripts.Length}");
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
                return null;
            }

            int oldPlayersCount = StartOfRound.Instance.allPlayerObjects.Length - AllInternAIs.Length;
            if (index < oldPlayersCount)
            {
                return null;
            }

            if (AllInternAIs.Length > 0)
            {
                return AllInternAIs[index == -1 ? 0 : index - oldPlayersCount];
            }
            return null;
        }

        public bool IsAIInternAi(EnemyAI ai)
        {
            if (AllInternAIs == null)
            {
                return false;
            }

            for(int i =0; i < AllInternAIs.Length; i++)
            {
                if (AllInternAIs[i] == ai)
                {
                    return true;
                }
            }

            return false;
        }

        public void ResizeAndPopulateInterns()
        {
            StartOfRound instance = StartOfRound.Instance;
            if (AllPlayerObjectsBackUp != null && AllPlayerObjectsBackUp.Length > 0)
            {
                if (instance.allPlayerObjects.Length == AllEntitiesCount)
                {
                    // the arrays have not been resize between round
                    return;
                }
            }
            else
            {
                AllInternAIs = new InternAI[Const.INTERN_AVAILABLE_MAX];
                AllPlayerObjectsBackUp = new GameObject[Const.INTERN_AVAILABLE_MAX];
                AllPlayerScriptsBackUp = new PlayerControllerB[Const.INTERN_AVAILABLE_MAX];
            }

            int irlPlayersCount = instance.allPlayerObjects.Length;
            int irlPlayersAndInternsCount = irlPlayersCount + Const.INTERN_AVAILABLE_MAX;
            Array.Resize(ref instance.allPlayerObjects, irlPlayersAndInternsCount);
            Array.Resize(ref instance.allPlayerScripts, irlPlayersAndInternsCount);
            Array.Resize(ref instance.gameStats.allPlayerStats, irlPlayersAndInternsCount);
            Array.Resize(ref instance.playerSpawnPositions, irlPlayersAndInternsCount);
            Plugin.Logger.LogDebug($"Resize for interns from irl players count of {irlPlayersCount} to {irlPlayersAndInternsCount}");
            AllEntitiesCount = irlPlayersAndInternsCount;

            GameObject internObjectParent = instance.allPlayerObjects[3].gameObject;
            for (int i = 0; i < AllPlayerObjectsBackUp.Length; i++)
            {
                if (AllPlayerObjectsBackUp[i] != null)
                {
                    Plugin.Logger.LogDebug($"use of backup : {AllPlayerScriptsBackUp[i].playerUsername}");
                    instance.allPlayerObjects[i + irlPlayersCount] = AllPlayerObjectsBackUp[i];
                    instance.allPlayerScripts[i + irlPlayersCount] = AllPlayerScriptsBackUp[i];
                    instance.gameStats.allPlayerStats[i + irlPlayersCount] = new PlayerStats();
                    instance.playerSpawnPositions[i + irlPlayersCount] = instance.playerSpawnPositions[3];
                    continue;
                }

                GameObject internNPC = Object.Instantiate<GameObject>(internObjectParent, Vector3.zero, Quaternion.identity);
                SpawnNetworkObjectsOfGameObject(internNPC);

                PlayerControllerB internController = internNPC.GetComponentInChildren<PlayerControllerB>();
                internController.isPlayerDead = false;
                internController.isPlayerControlled = false;
                internController.transform.localScale *= 0.85f;

                //todo unique name and unique id
                internController.playerClientId = (ulong)(i + irlPlayersCount);
                internController.actualClientId = internController.playerClientId;
                internController.playerUsername = string.Format("Intern #{0}", internController.playerClientId);
                internController.DropAllHeldItems(false, false);

                internNPC.SetActive(false);

                instance.allPlayerObjects[i + irlPlayersCount] = internNPC;
                instance.allPlayerScripts[i + irlPlayersCount] = internController;
                instance.gameStats.allPlayerStats[i + irlPlayersCount] = new PlayerStats();
                instance.playerSpawnPositions[i + irlPlayersCount] = instance.playerSpawnPositions[3];

                AllPlayerObjectsBackUp[i] = internNPC;
                AllPlayerScriptsBackUp[i] = internController;
            }

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

        private void SpawnNetworkObjectsOfGameObject(GameObject gameObject)
        {
            var listNetworkObjects = gameObject.GetComponentsInChildren<NetworkObject>();
            NetworkObject networkObjectRoot = null!;
            List<Tuple<NetworkObject, Transform>> listTupleTransformParentChild = new List<Tuple<NetworkObject, Transform>>();
            foreach (NetworkObject networkObject in listNetworkObjects)
            {
                if (networkObject.transform.parent == null)
                {
                    networkObjectRoot = networkObject;
                    continue;
                }

                listTupleTransformParentChild.Add(new Tuple<NetworkObject, Transform>(networkObject, networkObject.transform.parent));
                networkObject.Spawn(true);
            }
            networkObjectRoot.Spawn(true);

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
    }
}
