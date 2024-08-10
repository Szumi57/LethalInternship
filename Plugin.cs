using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Patches.EnemiesPatches;
using LethalInternship.Patches.GameEnginePatches;
using LethalInternship.Patches.MapHazardsPatches;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.ModPatches;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Patches.ObjectsPatches;
using LethalInternship.Patches.TerminalPatches;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship
{
    /// <summary>
    /// Main mod plugin class, start everything
    /// </summary>
    [BepInPlugin(ModGUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
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

            var bundleName = "internnpcmodassets";
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
                                                                                            //&& x.name != "MapDot"
                                                                                            //&& x.name != "Collision"
                                                                                            && x.name != "TurnCompass"
                                                                                            && x.name != "CreatureSFX"
                                                                                            && x.name != "CreatureVoice"
                                                                                            )
                                                               .ToList())
            {
                Object.DestroyImmediate(transform.gameObject);
            }

            // randomize GlobalObjectIdHash
            byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + gameObject.name + bundleName));
            uint newGlobalObjectIdHash = BitConverter.ToUInt32(value, 0);
            Type type = typeof(NetworkObject);
            FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);
            var networkObject = InternNPCPrefab.enemyPrefab.GetComponent<NetworkObject>();
            fieldInfo.SetValue(networkObject, newGlobalObjectIdHash);

            // Register the network prefab with the randomized GlobalObjectIdHash
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(InternNPCPrefab.enemyPrefab);

            InitPluginManager();

            PatchBaseGame();

            PatchOtherMods();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void PatchBaseGame()
        {
            // Game engine
            _harmony.PatchAll(typeof(DebugPatch));
            _harmony.PatchAll(typeof(GameNetworkManagerPatch));
            _harmony.PatchAll(typeof(HUDManagerPatch));
            _harmony.PatchAll(typeof(NetworkSceneManagerPatch));
            _harmony.PatchAll(typeof(NetworkObjectPatch));
            _harmony.PatchAll(typeof(RoundManagerPatch));
            _harmony.PatchAll(typeof(SoundManagerPatch));
            _harmony.PatchAll(typeof(StartOfRoundPatch));

            // Npc
            _harmony.PatchAll(typeof(EnemyAIPatch));
            _harmony.PatchAll(typeof(PlayerControllerBPatch));

            // Enemies
            _harmony.PatchAll(typeof(BaboonBirdAIPatch));
            _harmony.PatchAll(typeof(BlobAIPatch));
            _harmony.PatchAll(typeof(BushWolfEnemyPatch));
            _harmony.PatchAll(typeof(ButlerBeesEnemyAIPatch));
            _harmony.PatchAll(typeof(ButlerEnemyAIPatch));
            _harmony.PatchAll(typeof(CentipedeAIPatch));
            _harmony.PatchAll(typeof(CrawlerAIPatch));
            _harmony.PatchAll(typeof(FlowermanAIPatch));
            _harmony.PatchAll(typeof(FlowerSnakeEnemyPatch));
            _harmony.PatchAll(typeof(ForestGiantAIPatch));
            _harmony.PatchAll(typeof(JesterAIPatch));
            _harmony.PatchAll(typeof(MaskedPlayerEnemyPatch));
            _harmony.PatchAll(typeof(MouthDogAIPatch));
            _harmony.PatchAll(typeof(RadMechMissilePatch));
            _harmony.PatchAll(typeof(RedLocustBeesPatch));
            _harmony.PatchAll(typeof(SandSpiderAIPatch));
            _harmony.PatchAll(typeof(SandWormAIPatch));
            _harmony.PatchAll(typeof(SpringManAIPatch));

            // Map hazards
            _harmony.PatchAll(typeof(LandminePatch));
            _harmony.PatchAll(typeof(QuicksandTriggerPatch));
            _harmony.PatchAll(typeof(SpikeRoofTrapPatch));
            _harmony.PatchAll(typeof(TurretPatch));

            // Map
            _harmony.PatchAll(typeof(DoorLockPatch));
            _harmony.PatchAll(typeof(InteractTriggerPatch));
            _harmony.PatchAll(typeof(ItemDropShipPatch));
            _harmony.PatchAll(typeof(VehicleControllerPatch));

            // Objects
            _harmony.PatchAll(typeof(DeadBodyInfoPatch));
            _harmony.PatchAll(typeof(ShotgunItemPatch));
            _harmony.PatchAll(typeof(StunGrenadeItemPatch));

            // Terminal
            _harmony.PatchAll(typeof(TerminalPatch));

            //_harmony.PatchAll(typeof(MyPatches));
        }

        private void PatchOtherMods()
        {
            bool isModMoreEmoteLoaded = IsModLoaded(Const.MOREEMOTES_GUID);
            bool isModMoreCompanyLoaded = IsModLoaded(Const.MORECOMPANY_GUID);
            bool isModModelReplacementAPILoaded = IsModLoaded(Const.MODELREPLACEMENT_GUID);
            bool isModLethalPhonesLoaded = IsModLoaded(Const.LETHALPHONES_GUID);
            bool isModFasterItemDropshipLoaded = IsModLoaded(Const.FASTERITEMDROPSHIP_GUID);
            bool isModAdditionalNetworkingLoaded = IsModLoaded(Const.ADDITIONALNETWORKING_GUID);
            bool isModShowCapacityLoaded = IsModLoaded(Const.SHOWCAPACITY_GUID);

            // Compatibility with other mods
            if (isModMoreEmoteLoaded)
            {
                _harmony.PatchAll(typeof(MoreEmotesPatch));
            }
            if (isModModelReplacementAPILoaded && isModMoreCompanyLoaded)
            {
                _harmony.PatchAll(typeof(MoreCompanyCosmeticManagerPatch));
            }
            if (isModLethalPhonesLoaded)
            {
                _harmony.PatchAll(typeof(PlayerPhonePatchPatch));
                _harmony.PatchAll(typeof(PhoneBehaviorPatch));
                _harmony.PatchAll(typeof(PlayerPhonePatchLI));
            }
            if (isModFasterItemDropshipLoaded)
            {
                _harmony.PatchAll(typeof(FasterItemDropshipPatch));
            }
            if (isModAdditionalNetworkingLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("AdditionalNetworking.Patches.Inventory.PlayerControllerBPatch"), "OnStart"), 
                               null,
                               null, 
                               new HarmonyMethod(typeof(AdditionalNetworkingPatch), nameof(AdditionalNetworkingPatch.Start_Transpiler)));
            }
            if (isModShowCapacityLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ShowCapacity.Patches.PlayerControllerBPatch"), "Update_PreFix"),
                               new HarmonyMethod(typeof(ShowCapacityPatch), nameof(ShowCapacityPatch.Update_PreFix_Prefix)));
            }
        }

        private bool IsModLoaded(string modGUID)
        {
            bool ret = Chainloader.PluginInfos.ContainsKey(modGUID);
            if (ret)
            {
                Logger.LogDebug($"Mod loaded : GUID {modGUID}");
            }

            return ret;
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

        internal static void LogWarning(string warningLog)
        {
            Logger.LogWarning(warningLog);
        }

        internal static void LogError(string errorLog)
        {
            Logger.LogError(errorLog);
        }
    }
}