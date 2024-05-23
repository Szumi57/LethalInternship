using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Patches;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;


namespace LethalInternship
{
    [BepInPlugin(ModGUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
        // It is a good idea for our GUID to be more unique than only the plugin name. Notice that it is used in the BepInPlugin attribute.
        // The GUID is also used for the config file name by default.
        public const string ModGUID = "Szumi57." + PluginInfo.PLUGIN_NAME;

        public static AssetBundle ModAssets = null!;        
        
        internal static new ManualLogSource Logger = null!;
        internal static Config BoundConfig { get; private set; } = null!;

        private readonly Harmony _harmony = new(ModGUID);

        private void Awake() {

            Logger = base.Logger;

            BoundConfig = new Config(base.Config);

            // This should be ran before Network Prefabs are registered.
            InitializeNetworkBehaviours();
            
            // We load the asset bundle that should be next to our DLL file, with the specified name.
            // You may want to rename your asset bundle from the AssetBundle Browser in order to avoid an issue with
            // asset bundle identifiers being the same between multiple bundles, allowing the loading of only one bundle from one mod.
            // In that case also remember to change the asset bundle copying code in the csproj.user file.
            var bundleName = "modassets";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null) {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            //var ExampleEnemyTN = ModAssets.LoadAsset<TerminalNode>("ExampleEnemyTN");
            //var ExampleEnemyTK = ModAssets.LoadAsset<TerminalKeyword>("ExampleEnemyTK");

            // Optionally, we can list which levels we want to add our enemy to, while also specifying the spawn weight for each.
            /*
            var ExampleEnemyLevelRarities = new Dictionary<Levels.LevelTypes, int> {
                {Levels.LevelTypes.ExperimentationLevel, 10},
                {Levels.LevelTypes.AssuranceLevel, 40},
                {Levels.LevelTypes.VowLevel, 20},
                {Levels.LevelTypes.OffenseLevel, 30},
                {Levels.LevelTypes.MarchLevel, 20},
                {Levels.LevelTypes.RendLevel, 50},
                {Levels.LevelTypes.DineLevel, 25},
                // {Levels.LevelTypes.TitanLevel, 33},
                // {Levels.LevelTypes.All, 30},     // Affects unset values, with lowest priority (gets overridden by Levels.LevelTypes.Modded)
                {Levels.LevelTypes.Modded, 60},     // Affects values for modded moons that weren't specified
            };
            // We can also specify custom level rarities
            var ExampleEnemyCustomLevelRarities = new Dictionary<string, int> {
                {"EGyptLevel", 50},
                {"46 Infernis", 69},    // Either LLL or LE(C) name can be used, LethalLib will handle both
            };
            */

            // Network Prefabs need to be registered. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            // LethalLib registers prefabs on GameNetworkManager.Start.
            //NetworkPrefabs.RegisterNetworkPrefab(ExampleEnemy.enemyPrefab);

            // For different ways of registering your enemy, see https://github.com/EvaisaDev/LethalLib/blob/main/LethalLib/Modules/Enemies.cs
            //Enemies.RegisterEnemy(ExampleEnemy, BoundConfig.SpawnWeight.Value, Levels.LevelTypes.All, ExampleEnemyTN, ExampleEnemyTK);
            // For using our rarity tables, we can use the following:
            // Enemies.RegisterEnemy(ExampleEnemy, ExampleEnemyLevelRarities, ExampleEnemyCustomLevelRarities, ExampleEnemyTN, ExampleEnemyTK);
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            InternManager.Init();
            _harmony.PatchAll(typeof(StartOfRoundPatch));
            _harmony.PatchAll(typeof(ItemDropShipPatch));
            _harmony.PatchAll(typeof(PlayerControllerBPatch));
            _harmony.PatchAll(typeof(DoorLockPatch));
            _harmony.PatchAll(typeof(EnemyAIPatch));
            _harmony.PatchAll(typeof(SoundManagerPatch));
            _harmony.PatchAll(typeof(NetworkSceneManagerPatch));

            _harmony.PatchAll(typeof(GrabbableObjectPatch));
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}