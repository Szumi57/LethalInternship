using GameNetcodeStuff;
using LethalInternship.AI;
using LethalInternship.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship
{
    internal static class InternManager
    {
        public static InternAI[] AllInternAIs = null!;
        public static EnemyType InternNPCPrefab = null!;

        private static GameObject[] AllPlayerObjectsBackUp = null!;
        private static PlayerControllerB[] AllPlayerScriptsBackUp = null!;

        public static void Init()
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

        public static void SpawnIntern(Transform positionTransform, bool isOutside = true)
        {
            StartOfRound instance = StartOfRound.Instance;
            int indexNextPlayerObject = InternManager.GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                Plugin.Logger.LogInfo($"No more intern available");
                return;
            }
            Plugin.Logger.LogDebug($"indexNextPlayerObject {indexNextPlayerObject}");

            Vector3 spawnPosition = positionTransform.position;
            float yRot = positionTransform.eulerAngles.y;
            Plugin.Logger.LogDebug($"position : {spawnPosition}, yRot: {yRot}");

            PlayerControllerB internController = instance.allPlayerScripts[indexNextPlayerObject];
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.health = 50;
            internController.isInsideFactory = !isOutside;

            GameObject internObjectParent = instance.allPlayerObjects[indexNextPlayerObject];
            internObjectParent.transform.position = spawnPosition;
            internObjectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, yRot, 0f));

            int indexNextIntern = indexNextPlayerObject - (instance.allPlayerScripts.Length - InternManager.AllInternAIs.Length);
            Plugin.Logger.LogDebug($"Adding AI for intern {indexNextIntern} for body {indexNextPlayerObject}");
            GameObject internPrefab = Object.Instantiate<GameObject>(InternManager.InternNPCPrefab.enemyPrefab);
            internPrefab.GetComponentInChildren<NetworkObject>().Spawn(true);
            InternAI internAI = internPrefab.GetComponent<InternAI>();
            // Plug ai on intern body
            internAI.transform.parent = internObjectParent.transform;

            internAI.SetEnemyOutside(isOutside);
            internAI.creatureAnimator = internController.playerBodyAnimator;
            internAI.NpcController = new NpcController(internController);
            internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");
            internAI.LineRenderer = internAI.gameObject.AddComponent<LineRenderer>();
            internAI.ventAnimationFinished = true;
            internAI.transform.position = internController.transform.position;
            internAI.enabled = true;
            InternManager.AllInternAIs[indexNextIntern] = internAI;

            internObjectParent.SetActive(true);
            //GameObject internNPC = Object.Instantiate<GameObject>(internObjectParent, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));

            //ComponentUtil.ListAllComponents(internNPC);
            //PropertiesUtils.ListProperties(internNPC.GetComponentInChildren<PlayerControllerB>());
            //PropertiesUtils.ListProperties(internNPC.GetComponentInChildren<CharacterController>());
        }

        private static int GetNextAvailablePlayerObject()
        {
            StartOfRound instance = StartOfRound.Instance;
            //Plugin.Logger.LogDebug($"2 instance.allPlayerScripts.Length : {instance.allPlayerScripts.Length}");
            //Plugin.Logger.LogDebug($"2 instance.allPlayerObjects.Length : {instance.allPlayerScripts.Length}");
            for (int i = instance.allPlayerScripts.Length - AllInternAIs.Length; i < instance.allPlayerScripts.Length; i++)
            {
                if (!instance.allPlayerScripts[i].isPlayerControlled)
                {
                    return i;
                }
            }
            return -1;
        }

        public static InternAI? GetInternAI(int index)
        {
            //Plugin.Logger.LogDebug($"1 instance.allPlayerScripts.Length : {StartOfRound.Instance.allPlayerScripts.Length}");
            //Plugin.Logger.LogDebug($"1 instance.allPlayerObjects.Length : {StartOfRound.Instance.allPlayerObjects.Length}");
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

        public static void ResizeAndPopulateInterns()
        {
            int internCount = 16;

            StartOfRound instance = StartOfRound.Instance;
            int oldPlayersCount = instance.allPlayerObjects.Length;
            int playerAndInternCount = oldPlayersCount + internCount;
            Array.Resize(ref instance.allPlayerObjects, playerAndInternCount);
            Array.Resize(ref instance.allPlayerScripts, playerAndInternCount);
            Array.Resize(ref instance.gameStats.allPlayerStats, playerAndInternCount);
            Array.Resize(ref instance.playerSpawnPositions, playerAndInternCount);
            Plugin.Logger.LogDebug($"Resize for interns from {oldPlayersCount} to {playerAndInternCount}");

            if (AllPlayerObjectsBackUp != null && AllPlayerObjectsBackUp.Length > 0)
            {
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

                for (int i = 0; i < AllPlayerObjectsBackUp.Length; i++)
                {
                    instance.allPlayerObjects[i + oldPlayersCount] = AllPlayerObjectsBackUp[i];
                    instance.allPlayerScripts[i + oldPlayersCount] = AllPlayerScriptsBackUp[i];
                    instance.gameStats.allPlayerStats[i + oldPlayersCount] = new PlayerStats();
                    instance.playerSpawnPositions[i + oldPlayersCount] = instance.playerSpawnPositions[3];
                }
            }
            else
            {
                AllInternAIs = new InternAI[internCount];
                AllPlayerObjectsBackUp = new GameObject[internCount];
                AllPlayerScriptsBackUp = new PlayerControllerB[internCount];
                GameObject internObjectParent = instance.allPlayerObjects[3].gameObject;

                for (int i = oldPlayersCount; i < playerAndInternCount; i++)
                {
                    GameObject internNPC = Object.Instantiate<GameObject>(internObjectParent, Vector3.zero, Quaternion.identity);
                    SpawnNetworkObjectsOfGameObject(internNPC);

                    PlayerControllerB internController = internNPC.GetComponentInChildren<PlayerControllerB>();
                    internController.isPlayerDead = false;
                    internController.isPlayerControlled = false;
                    internController.transform.localScale *= 0.85f;

                    //todo unique name and unique id
                    internController.playerClientId = (ulong)i;
                    internController.actualClientId = internController.playerClientId;
                    internController.DropAllHeldItems(false, false);

                    internNPC.SetActive(false);

                    instance.allPlayerObjects[i] = internNPC;
                    instance.allPlayerScripts[i] = internController;
                    instance.gameStats.allPlayerStats[i] = new PlayerStats();
                    instance.playerSpawnPositions[i] = instance.playerSpawnPositions[3];

                    AllPlayerObjectsBackUp[i - oldPlayersCount] = internNPC;
                    AllPlayerScriptsBackUp[i - oldPlayersCount] = internController;
                }
            }
        }

        private static void SpawnNetworkObjectsOfGameObject(GameObject gameObject)
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
    }
}
