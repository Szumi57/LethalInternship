using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Patches.EnemiesPatches;
using LethalInternship.Patches.GameEnginePatches;
using LethalInternship.Patches.MapHazardsPatches;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Patches.ObjectsPatches;
using LethalInternship.Patches.TerminalPatches;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalInternship
{
    /// <summary>
    /// Main mod plugin class, start everything
    /// </summary>
    [BepInPlugin(ModGUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "Szumi57." + PluginInfo.PLUGIN_NAME;

        public static AssetBundle ModAssets = null!;

        internal static EnemyType InternNPCPrefab = null!;
        internal static int IrlPlayersCount = 0;

        internal static Config BoundConfig { get; private set; } = null!;

        private static new ManualLogSource Logger = null!;
        private readonly Harmony _harmony = new(ModGUID);

        private void Awake()
        {
            Logger = base.Logger;

            BoundConfig = new Config(base.Config);

            // This should be ran before Network Prefabs are registered.
            InitializeNetworkBehaviours();

            var bundleName = "modassets";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // Load intern prefab
            InternNPCPrefab = Plugin.ModAssets.LoadAsset<EnemyType>("InternNPC");
            if (InternNPCPrefab == null)
            {
                Logger.LogError($"InternNPC prefab.");
                return;
            }
            foreach (var transform in InternNPCPrefab.enemyPrefab.GetComponentsInChildren<Transform>()
                                                               .Where(x => x.parent != null && x.parent.name == "InternNPCObj"
                                                                                            //&& x.name != "ScanNode"
                                                                                            && x.name != "MapDot"
                                                                                            //&& x.name != "Collision"
                                                                                            && x.name != "TurnCompass"
                                                                                            && x.name != "CreatureSFX"
                                                                                            && x.name != "CreatureVoice"
                                                                                            )
                                                               .ToList())
            {
                Object.DestroyImmediate(transform.gameObject);
            }
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(InternNPCPrefab.enemyPrefab);

            InitPluginManager();

            // Game engine
            _harmony.PatchAll(typeof(DebugPatch));
            _harmony.PatchAll(typeof(GameNetworkManagerPatch));
            _harmony.PatchAll(typeof(HUDManagerPatch));
            _harmony.PatchAll(typeof(NetworkSceneManagerPatch));
            _harmony.PatchAll(typeof(NetworkObjectPatch));
            _harmony.PatchAll(typeof(SoundManagerPatch));
            _harmony.PatchAll(typeof(StartOfRoundPatch));

            // Npc
            _harmony.PatchAll(typeof(EnemyAIPatch));
            _harmony.PatchAll(typeof(PlayerControllerBPatch));

            // Enemies
            _harmony.PatchAll(typeof(BaboonBirdAIPatch));
            _harmony.PatchAll(typeof(BlobAIPatch));
            _harmony.PatchAll(typeof(ButlerBeesEnemyAIPatch));
            _harmony.PatchAll(typeof(ButlerEnemyAIPatch));
            _harmony.PatchAll(typeof(CentipedeAIPatch));
            _harmony.PatchAll(typeof(CrawlerAIPatch));
            _harmony.PatchAll(typeof(FlowerSnakeEnemyPatch));
            _harmony.PatchAll(typeof(ForestGiantAIPatch));
            _harmony.PatchAll(typeof(MouthDogAIPatch));
            _harmony.PatchAll(typeof(RedLocustBeesPatch));
            _harmony.PatchAll(typeof(SandSpiderAIPatch));
            _harmony.PatchAll(typeof(SandWormAIPatch));
            _harmony.PatchAll(typeof(SpringManAIPatch));

            // Map
            _harmony.PatchAll(typeof(DoorLockPatch));
            _harmony.PatchAll(typeof(InteractTriggerPatch));
            _harmony.PatchAll(typeof(ItemDropShipPatch));

            // Map hazards
            _harmony.PatchAll(typeof(LandminePatch));
            _harmony.PatchAll(typeof(QuicksandTriggerPatch));
            _harmony.PatchAll(typeof(SpikeRoofTrapPatch));
            _harmony.PatchAll(typeof(TurretPatch));

            // Objects
            _harmony.PatchAll(typeof(DeadBodyInfoPatch));
            _harmony.PatchAll(typeof(ShotgunItemPatch));

            // Terminal
            _harmony.PatchAll(typeof(TerminalPatch));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
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

        private static void InitPluginManager()
        {
            GameObject gameObject = new GameObject("PluginManager");
            gameObject.AddComponent<PluginManager>();
            PluginManager.Instance.InitManagers();
        }

        internal static void LogDebug(string debugLog)
        {
            if (!BoundConfig.EnableDebugLog.Value)
            {
                return;
            }
            Logger.LogDebug(debugLog);
        }

        internal static void LogInfo(string infoLog)
        {
            Logger.LogInfo(infoLog);
        }

        internal static void LogError(string errorLog)
        {
            Logger.LogError(errorLog);
        }
    }
}