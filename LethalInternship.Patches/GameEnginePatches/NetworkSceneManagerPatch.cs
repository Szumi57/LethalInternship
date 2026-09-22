using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>NetworkSceneManager</c>
    /// </summary>
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    public class NetworkSceneManagerPatch
    {
        /// <summary>
        /// Patch for populate the pool of interns at the start of the load scene
        /// </summary>
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix(ref Dictionary<uint, Dictionary<int, NetworkObject>> ___ScenePlacedObjects)
        {
            PopulateSceneAndNetwork(___ScenePlacedObjects);
        }

        private static void PopulateSceneAndNetwork(Dictionary<uint, Dictionary<int, NetworkObject>> ScenePlacedObjects)
        {
            var context = PluginRuntimeProvider.Context;
            StartOfRound instanceSOR = StartOfRound.Instance;

            // Before populating
            int previousIrlCount = context.PreparedIrlPlayersCount;
            int previousInternCount = context.PreparedInternCount;

            // Current state
            if (PluginRuntimeProvider.Context.IsModMoreCompanyLoaded)
                UpdateIrlPlayerAfterMoreCompany(); // In another method to not load a type of an non loaded dll (like moreCompany)

            int newIrlCount = context.PluginIrlPlayersCount;
            int newInternCount = context.Config.MaxInternsAvailable;

            // Array of intern objects
            var internObjects = context.InternObjects;
            Array.Resize(ref internObjects, newInternCount);

            // Array of network hashes
            var internNetworkObjectHashes = PluginRuntimeProvider.Context.InternNetworkObjectHashes;
            Array.Resize(ref internNetworkObjectHashes, newInternCount);

            // Resizing base game arrays
            int totalCount = newIrlCount + newInternCount;
            Array.Resize(ref instanceSOR.allPlayerObjects, totalCount);
            Array.Resize(ref instanceSOR.allPlayerScripts, totalCount);
            Array.Resize(ref instanceSOR.gameStats.allPlayerStats, totalCount);
            Array.Resize(ref instanceSOR.playerSpawnPositions, totalCount);
            PluginRuntimeProvider.Context.AllEntitiesCount = totalCount;
            PluginLoggerHook.LogDebug?.Invoke($"Resized arrays from (irl + interns) {previousIrlCount} + {previousInternCount} = {previousIrlCount + previousInternCount} to {newIrlCount} + {newInternCount} = {totalCount}");

            for (int internIndex = 0; internIndex < newInternCount; internIndex++)
            {
                int playerIndex = newIrlCount + internIndex;
                GameObject internObject = internObjects[internIndex];
                if (internObject == null)
                {
                    PluginLoggerHook.LogInfo?.Invoke($"Populating intern array with new body {internIndex + 1}");
                    GameObject template = instanceSOR.allPlayerObjects[newIrlCount - 1];
                    internObject = Object.Instantiate<GameObject>(template, template.transform.parent);
                    internObjects[internIndex] = internObject;
                }

                SetupIntern(instanceSOR, internObject, playerIndex, newIrlCount);

                // Populate the network
                NetworkObject[] networkObjects = internObject.GetComponentsInChildren<NetworkObject>();
                // First hashes creation
                if (internNetworkObjectHashes[internIndex] == null)
                {
                    internNetworkObjectHashes[internIndex] = new uint[networkObjects.Length];

                    for (int networkIndex = 0; networkIndex < networkObjects.Length; networkIndex++)
                    {
                        internNetworkObjectHashes[internIndex][networkIndex] = GetNextInternNetworkHash();
                    }
                }

                uint[] hashes = internNetworkObjectHashes[internIndex];
                if (hashes.Length != networkObjects.Length)
                {
                    PluginLoggerHook.LogError?.Invoke($"Intern {internIndex} has {networkObjects.Length} NetworkObjects but {hashes.Length} hashes.");
                    continue;
                }

                for (int networkIndex = 0; networkIndex < networkObjects.Length; networkIndex++)
                {
                    NetworkObject networkObject = networkObjects[networkIndex];
                    uint hash = hashes[networkIndex];

                    RegisterNetworkObject(networkObject, hash, ScenePlacedObjects);
                }
            }

            PluginRuntimeProvider.Context.NextInternNetworkObjectHash = 100001;
            context.InternObjects = internObjects;
            context.PreparedIrlPlayersCount = newIrlCount;
            context.PreparedInternCount = newInternCount;
        }

        private static void SetupIntern(StartOfRound sor,
                                        GameObject internObject,
                                        int playerIndex,
                                        int newIrlCount)
        {
            PlayerControllerB internController = internObject.GetComponentInChildren<PlayerControllerB>();
            internController.playerClientId = (ulong)(playerIndex);
            internController.isPlayerDead = false;
            internController.isPlayerControlled = false;
            internController.transform.localScale = new Vector3(PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale);
            internController.thisController.radius *= PluginRuntimeProvider.Context.Config.InternSizeScale;
            internController.actualClientId = internController.playerClientId + Const.INTERN_ACTUAL_ID_OFFSET;
            internController.playerUsername = string.Format(ConfigConst.DEFAULT_INTERN_NAME, internController.playerClientId - (ulong)newIrlCount);

            sor.allPlayerObjects[playerIndex] = internObject;
            sor.allPlayerScripts[playerIndex] = internController;
            sor.gameStats.allPlayerStats[playerIndex] = new PlayerStats();
            sor.playerSpawnPositions[playerIndex] = sor.playerSpawnPositions[3];
        }

        private static uint GetNextInternNetworkHash()
        {
            uint hash = PluginRuntimeProvider.Context.NextInternNetworkObjectHash;
            PluginRuntimeProvider.Context.NextInternNetworkObjectHash++;
            return hash;
        }

        private static void RegisterNetworkObject(NetworkObject networkObject,
                                                  uint globalObjectIdHash,
                                                  Dictionary<uint, Dictionary<int, NetworkObject>> scenePlacedObjects)
        {
            if (networkObject.IsSpawned)
            {
                //Debug.Log($"Skipping already spawned NetworkObject: {networkObject.name}");
                return;
            }

            PatchesUtil.SetFieldValue(networkObject, "GlobalObjectIdHash", globalObjectIdHash);

            int handle = networkObject.gameObject.scene.handle;

            if (!scenePlacedObjects.TryGetValue(globalObjectIdHash, out Dictionary<int, NetworkObject>? objectsByScene))
            {
                objectsByScene = new Dictionary<int, NetworkObject>();
                scenePlacedObjects.Add(globalObjectIdHash, objectsByScene);
            }

            if (objectsByScene.TryGetValue(handle, out NetworkObject? existingObject))
            {
                if (existingObject == networkObject)
                    return;

                PluginLoggerHook.LogError?.Invoke($"{networkObject.name} tried to register with GlobalObjectIdHash {globalObjectIdHash}, " +
                                                  $"but it is already used by {existingObject?.name ?? "Null Entry"}.");
                return;
            }

            //PluginLoggerHook.LogInfo?.Invoke($"Populating network with intern globalObjectIdHash {globalObjectIdHash}");
            objectsByScene.Add(handle, networkObject);
        }

        private static void UpdateIrlPlayerAfterMoreCompany()
        {
            PluginRuntimeProvider.Context.PluginIrlPlayersCount = MoreCompany.MainClass.newPlayerCount;
            PluginLoggerHook.LogDebug?.Invoke($"PluginIrlPlayersCount after morecompany = {PluginRuntimeProvider.Context.PluginIrlPlayersCount}");
        }
    }
}
