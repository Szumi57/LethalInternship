using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;
using System.Runtime.CompilerServices;
using System.Linq;
using LethalInternship.Utils;
using GameNetcodeStuff;
using UnityEngine.AI;
using LethalInternship.AI;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static EnemyType intern = null!;
        private static EnemyType? enemyTypeMaskedEnemy = null!;

        public static void Init()
        {
            //
            intern = Plugin.ModAssets.LoadAsset<EnemyType>("InternNPC");
            if (intern != null)
            {
                foreach (var transform in intern.enemyPrefab.GetComponentsInChildren<Transform>()
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

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref StartOfRound __instance)
        {
            Plugin.Logger.LogDebug("StartOfRound start, try to spawn");

            if (!__instance.IsServer)
            {
                return;
            }

            //TreesUtils.PrintComponentsTreeOfGameObject(__instance.allPlayerScripts[0].gameObject);

            // Looking for masked enemy in loaded objects
            if (enemyTypeMaskedEnemy == null)
            {
                EnemyType[] enemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
                if (enemyTypes.Length == 0)
                {
                    Plugin.Logger.LogError("No enemy types found.");
                    return;
                }
                enemyTypeMaskedEnemy = enemyTypes.FirstOrDefault(x => x.name == "MaskedPlayerEnemy");
            }

            //todo supprimer
            //Plugin.Logger.LogDebug("Enemy types found : ");
            //for (int i = 0; i < enemyTypes.Length; i++)
            //{
            //    Plugin.Logger.LogDebug(enemyTypes[i].name);
            //}

            // get a spawn position inside ship

            // Spawn intern
            //SpawnIntern(__instance.playerSpawnPositions[1]);
            //SpawnIntern(__instance.playerSpawnPositions[2]);
        }

        public static void SpawnIntern(Transform positionTransform)
        {
            // Method 1
            Vector3 spawnPosition = positionTransform.position;
            float yRot = positionTransform.eulerAngles.y;
            Plugin.Logger.LogDebug($"position : {spawnPosition}, yRot: {yRot}");



            GameObject gameObjectToSpawn = StartOfRound.Instance.allPlayerObjects[0].gameObject;
            GameObject internNPC = Object.Instantiate<GameObject>(gameObjectToSpawn, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));

            var listNetworkObjects = internNPC.GetComponentsInChildren<NetworkObject>();
            NetworkObject networkObjectRoot = null!;
            foreach (var networkObject in listNetworkObjects)
            {
                if (networkObject.transform.parent == null)
                {
                    networkObjectRoot = networkObject;
                    continue;
                }

                networkObject.Spawn(true);
            }
            networkObjectRoot.Spawn(true);
            Plugin.Logger.LogDebug($"---------------");



            Plugin.Logger.LogDebug($"Adding AI");
            GameObject internGameobject = Object.Instantiate<GameObject>(intern.enemyPrefab);
            internGameobject.GetComponentInChildren<NetworkObject>().Spawn(true);

            var playerController = internNPC.GetComponentInChildren<PlayerControllerB>();
            playerController.isPlayerControlled = true;
            Plugin.Logger.LogDebug($"Local position playerController {playerController.transform.localPosition}");
            playerController.transform.localScale *= 0.85f;
            //todo unique name
            playerController.playerUsername = "Intern";

            var aiPrefab = internGameobject.GetComponent<InternAI>();

            //Plugin.Logger.LogDebug($"---------------");
            //ComponentUtil.ListAllComponents(exampleEnemyGameobject);

            aiPrefab.ventAnimationFinished = true;
            aiPrefab.creatureAnimator = playerController.playerBodyAnimator;
            aiPrefab.NpcController = aiPrefab.gameObject.AddComponent<NpcController>();
            aiPrefab.NpcController.Npc = playerController;
            aiPrefab.eye = internNPC.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");
            aiPrefab.transform.position = playerController.transform.position;

            aiPrefab.himself = internNPC;

            // Plug ai on intern
            aiPrefab.transform.parent = internNPC.transform;

            //ComponentUtil.ListAllComponents(internNPC);

            //PropertiesUtils.ListProperties(internNPC.GetComponentInChildren<PlayerControllerB>());
            //PropertiesUtils.ListProperties(internNPC.GetComponentInChildren<CharacterController>());

            return;
            // Method 2
            // Spawn masked
            Plugin.Logger.LogDebug($"Spawn masked");
            if (enemyTypeMaskedEnemy == null)
            {
                Plugin.Logger.LogError("No masked enemy type found.");
                return;
            }
            GameObject maskedGameobject = Object.Instantiate<GameObject>(enemyTypeMaskedEnemy.enemyPrefab);
            maskedGameobject.SetActive(false);
            Transform transformScavengerModel = maskedGameobject.GetComponentsInChildren<Transform>().First(x => x.name == "ScavengerModel");
            BoxCollider boxMasked = maskedGameobject.GetComponentsInChildren<BoxCollider>().First(x => x.name == "MaskedPlayerEnemy(Clone)");

            //TreesUtils.PrintComponentsTreeOfGameObject(maskedGameobject);

            // spawn agent
            Plugin.Logger.LogDebug($"Spawn intern");
            if (intern == null)
            {
                Plugin.Logger.LogError($"Intern is not initialized.");
                return;
            }

            GameObject exampleEnemyGameobject25151 = Object.Instantiate<GameObject>(intern.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
            internGameobject.GetComponentInChildren<NetworkObject>().Spawn(true);
            //-----
            maskedGameobject.transform.position = internGameobject.transform.position;
            maskedGameobject.transform.eulerAngles = internGameobject.transform.eulerAngles;
            transformScavengerModel.parent = internGameobject.transform;
            //-----

            //
            foreach (var transform in internGameobject.GetComponentsInChildren<Transform>()
                                                            .Where(x => x.parent != null && x.parent.name == "spine.004" && x.name != "spine.004_end")
                                                            .ToList())
            {
                Object.DestroyImmediate(transform.gameObject);
            }
            internGameobject.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            //
            BoxCollider box = internGameobject.GetComponentsInChildren<BoxCollider>().First(x => x.name == "Collision");
            box.center = boxMasked.center;
            box.size = boxMasked.size;

            PropertiesAndFieldsUtils.ListPropertiesAndFields(internGameobject.GetComponent<InternAI>());

            TreesUtils.PrintComponentsTreeOfGameObject(internGameobject);
        }
    }
}
