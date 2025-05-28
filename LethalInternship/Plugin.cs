using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LethalInternship.Inputs;
using LethalInternship.Managers;
using LethalInternship.PluginPatches.GameEnginePatches;
using LethalInternship.SharedAbstractions.Configs;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Events;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Inputs;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
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
    // HardDependencies
    [BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(Const.CSYNC_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LethalCompanyInputUtils.LethalCompanyInputUtilsPlugin.ModId, BepInDependency.DependencyFlags.HardDependency)]
    // SoftDependencies
    [BepInDependency(Const.REVIVECOMPANY_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.BUNKBEDREVIVE_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.ZAPRILLATOR_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.MOREEMOTES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.BETTEREMOTES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.TOOMANYEMOTES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.MORECOMPANY_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.MODELREPLACEMENT_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.LETHALPHONES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.FASTERITEMDROPSHIP_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.SHOWCAPACITY_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.RESERVEDITEMSLOTCORE_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.LETHALPROGRESSION_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.QUICKBUYMENU_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.BUTTERYFIXES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.PEEPERS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.LETHALMIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Const.HOTDOGMODEL_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "Szumi57." + PluginInfo.PLUGIN_NAME;

        public static AssetBundle ModAssets = null!;
        public static EnemyType InternNPCPrefab = null!;

        // UI
        internal static bool UIAssetsLoaded = false;
        public static GameObject CommandsSingleInternUIPrefab = null!;
        public static GameObject CommandsMultipleInternsUIPrefab = null!;

        public static GameObject WorldIconPrefab = null!;
        public static GameObject InputIconPrefab = null!;

        public static GameObject DefaultIconImagePrefab = null!;
        public static GameObject PointerIconImagePrefab = null!;
        public static GameObject PedestrianIconImagePrefab = null!;
        public static GameObject VehicleIconImagePrefab = null!;
        public static GameObject ShipIconImagePrefab = null!;
        public static GameObject MeetingPointIconImagePrefab = null!;
        public static GameObject GatheringPointIconImagePrefab = null!;
        public static GameObject AttackIconImagePrefab = null!;

        internal static string DirectoryName = null!;
        internal static new ManualLogSource Logger = null!;
        internal static new Configs.Config Config = null!;
        internal static ILethalInternshipInputs InputActionsInstance = null!;
        internal static int PluginIrlPlayersCount = 0;

        internal static bool IsModTooManyEmotesLoaded = false;
        internal static bool IsModModelReplacementAPILoaded = false;
        internal static bool IsModCustomItemBehaviourLibraryLoaded = false;
        internal static bool IsModMoreCompanyLoaded = false;
        internal static bool IsModReviveCompanyLoaded = false;
        internal static bool IsModBunkbedReviveLoaded = false;
        internal static bool IsModLethalMinLoaded = false;
        internal static bool IsModMipaLoaded = false;
        internal static bool IsModMonoProfilerLoaderLoaded = false;

        private readonly Harmony _harmony = new(ModGUID);

        private void Awake()
        {
            var bundleName = "internnpcmodassets";
            DirectoryName = Path.GetDirectoryName(Info.Location);

            Logger = base.Logger;

            Config = new Configs.Config(base.Config);
            InputActionsInstance = new LethalInternshipInputs();

            // This should be ran before Network Prefabs are registered.
            InitializeNetworkBehaviours();

            // Load mod assets from unity
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(DirectoryName, bundleName));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // Load intern prefab
            if (!LoadInternPrefab(bundleName))
            {
                return;
            }

            // Load UI prefabs
            UIAssetsLoaded = LoadUIPrefabs();

            InitSharedValues();

            InitPluginManager();

            PatchBaseGame();

            PatchOtherMods();

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
                    try
                    {
                        var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                        if (attributes.Length > 0)
                        {
                            method.Invoke(null, null);
                        }
                    }
                    catch { }
                }
            }
        }

        private bool LoadInternPrefab(string bundleName)
        {
            InternNPCPrefab = Plugin.ModAssets.LoadAsset<EnemyType>("InternNPC");
            if (InternNPCPrefab == null)
            {
                Logger.LogError($"Failed to load InternNPC prefab.");
                return false;
            }
            if (InternNPCPrefab.enemyPrefab == null)
            {
                Logger.LogError($"Failed to load InternNPCPrefab.enemyPrefab.");
                return false;
            }
            foreach (var transform in InternNPCPrefab.enemyPrefab.GetComponentsInChildren<Transform>()
                                                               .Where(x => x.parent != null && x.parent.name == "InternNPCObj"
                                                                                            //&& x.name != "ScanNode"
                                                                                            //&& x.name != "MapDot"
                                                                                            && x.name != "Collision"
                                                                                            && x.name != "TurnCompass"
                                                                                            && x.name != "CreatureSFX"
                                                                                            && x.name != "CreatureVoice"
                                                                                            )
                                                               .ToList())
            {
                Object.DestroyImmediate(transform.gameObject);
            }

            // Randomize GlobalObjectIdHash
            byte[] value = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + gameObject.name + bundleName));
            uint newGlobalObjectIdHash = BitConverter.ToUInt32(value, 0);
            Type type = typeof(NetworkObject);
            FieldInfo fieldInfo = type.GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);
            var networkObject = InternNPCPrefab.enemyPrefab.GetComponent<NetworkObject>();
            fieldInfo.SetValue(networkObject, newGlobalObjectIdHash);

            // Register the network prefab with the randomized GlobalObjectIdHash
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(InternNPCPrefab.enemyPrefab);

            return true;
        }

        private static void InitPluginManager()
        {
            GameObject gameObject = new GameObject("PluginManager");
            gameObject.AddComponent<PluginManager>();
            PluginManager.Instance.InitManagers();
        }

        private bool LoadUIPrefabs()
        {
            // Commands wheel
            CommandsSingleInternUIPrefab = Plugin.ModAssets.LoadAsset<GameObject>("CommandsSingleIntern");
            if (CommandsSingleInternUIPrefab == null)
            {
                Logger.LogError($"Failed to load Commands UI prefab.");
                return false;
            }

            CommandsMultipleInternsUIPrefab = Plugin.ModAssets.LoadAsset<GameObject>("CommandsMultipleInterns");
            if (CommandsMultipleInternsUIPrefab == null)
            {
                Logger.LogError($"Failed to load Commands UI prefab.");
                return false;
            }

            // Icon UI
            WorldIconPrefab = Plugin.ModAssets.LoadAsset<GameObject>("WorldIcon");
            if (WorldIconPrefab == null)
            {
                Logger.LogError($"Failed to load WorldIcon UI prefab.");
                return false;
            }

            InputIconPrefab = Plugin.ModAssets.LoadAsset<GameObject>("InputIcon");
            if (InputIconPrefab == null)
            {
                Logger.LogError($"Failed to load InputIcon UI prefab.");
                return false;
            }

            // Images prefabs
            PointerIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("PointerIconImage");
            if (PointerIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load PointerIconImage UI prefab.");
                return false;
            }

            DefaultIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("DefaultIconImage");
            if (DefaultIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load DefaultIconImage UI prefab.");
                return false;
            }

            PedestrianIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("PedestrianIconImage");
            if (PedestrianIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load PedestrianIconImage UI prefab.");
                return false;
            }

            VehicleIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("VehicleIconImage");
            if (VehicleIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load VehicleIconImage UI prefab.");
                return false;
            }

            ShipIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("ShipIconImage");
            if (ShipIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load ShipIconImage UI prefab.");
                return false;
            }

            MeetingPointIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("MeetingPointIconImage");
            if (MeetingPointIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load MeetingPointIconImage UI prefab.");
                return false;
            }

            GatheringPointIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("GatheringPointIconImage");
            if (GatheringPointIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load GatheringPointIconImage UI prefab.");
                return false;
            }

            AttackIconImagePrefab = Plugin.ModAssets.LoadAsset<GameObject>("AttackIconImage");
            if (AttackIconImagePrefab == null)
            {
                Logger.LogError($"Failed to load AttackIconImage UI prefab.");
                return false;
            }

            return true;
        }

        private void PatchBaseGame()
        {
            Assembly? patchesAssembly = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "LethalInternship.Patches")
                {
                    patchesAssembly = asm;
                    break;
                }
            }

            if (patchesAssembly == null)
            {
                LogError("LethalInternship.Patches not found ! Cannot apply patches !");
                return;
            }

            // Game engine
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.DebugPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.GameNetworkManagerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.HUDManagerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.NetworkSceneManagerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.NetworkObjectPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.RoundManagerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.SoundManagerPatch"));
            _harmony.PatchAll(typeof(StartOfRoundPatch));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.GameEnginePatches.StartOfRoundPatch"));

            // Npc
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.NpcPatches.EnemyAICollisionDetectPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.NpcPatches.EnemyAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.NpcPatches.PlayerControllerBPatch"));

            patchesAssembly.GetType("LethalInternship.Patches.NpcPatches.PlayerControllerBUtils")?.GetMethod("Init")?.Invoke(null, null);

            // Enemies
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.BaboonBirdAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.BlobAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.ButlerBeesEnemyAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.ButlerEnemyAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.CaveDwellerAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.CentipedeAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.CrawlerAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.FlowermanAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.FlowerSnakeEnemyPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.ForestGiantAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.JesterAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.MaskedPlayerEnemyPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.MouthDogAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.RadMechMissilePatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.RedLocustBeesPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.SandSpiderAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.SandWormAIPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.EnemiesPatches.SpringManAIPatch"));

            // Map hazards
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapHazardsPatches.LandminePatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapHazardsPatches.QuicksandTriggerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapHazardsPatches.SpikeRoofTrapPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapHazardsPatches.TurretPatch"));

            // Map
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.DoorLockPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.InteractTriggerPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.ItemDropShipPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.ManualCameraRendererPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.ShipTeleporterPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.MapPatches.VehicleControllerPatch"));

            patchesAssembly.GetType("LethalInternship.Patches.MapPatches.ShipTeleporterUtils")?.GetMethod("Init")?.Invoke(null, null);

            // Objects
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ObjectsPatches.DeadBodyInfoPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ObjectsPatches.GrabbableObjectPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ObjectsPatches.RagdollGrabbableObjectPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ObjectsPatches.ShotgunItemPatch"));
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ObjectsPatches.StunGrenadeItemPatch"));

            // Terminal
            _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.TerminalPatches.TerminalPatch"));
            patchesAssembly.GetType("LethalInternship.Patches.TerminalPatches.TerminalUtils")?.GetMethod("Init")?.Invoke(null, null);
        }

        private void PatchOtherMods()
        {
            // -----------------------
            // Are these mods loaded ?
            IsModTooManyEmotesLoaded = IsModLoaded(Const.TOOMANYEMOTES_GUID);
            IsModModelReplacementAPILoaded = IsModLoaded(Const.MODELREPLACEMENT_GUID);
            IsModCustomItemBehaviourLibraryLoaded = IsModLoaded(Const.CUSTOMITEMBEHAVIOURLIBRARY_GUID);
            IsModMoreCompanyLoaded = IsModLoaded(Const.MORECOMPANY_GUID);
            IsModReviveCompanyLoaded = IsModLoaded(Const.REVIVECOMPANY_GUID);
            IsModBunkbedReviveLoaded = IsModLoaded(Const.BUNKBEDREVIVE_GUID);
            IsModLethalMinLoaded = IsModLoaded(Const.LETHALMIN_GUID);
            IsModMipaLoaded = IsModLoaded(Const.MIPA_GUID);

            bool isModMoreEmotesLoaded = IsModLoaded(Const.MOREEMOTES_GUID);
            bool isModBetterEmotesLoaded = IsModLoaded(Const.BETTEREMOTES_GUID);
            bool isModLethalPhonesLoaded = IsModLoaded(Const.LETHALPHONES_GUID);
            bool isModFasterItemDropshipLoaded = IsModLoaded(Const.FASTERITEMDROPSHIP_GUID);
            bool isModShowCapacityLoaded = IsModLoaded(Const.SHOWCAPACITY_GUID);
            bool isModReservedItemSlotCoreLoaded = IsModLoaded(Const.RESERVEDITEMSLOTCORE_GUID);
            bool isModLethalProgressionLoaded = IsModLoaded(Const.LETHALPROGRESSION_GUID);
            bool isModQuickBuyLoaded = IsModLoaded(Const.QUICKBUYMENU_GUID);
            bool isModLCAlwaysHearWalkieModLoaded = IsModLoaded(Const.LCALWAYSHEARWALKIEMOD_GUID);
            bool isModZaprillatorLoaded = IsModLoaded(Const.ZAPRILLATOR_GUID);
            bool isModButteryFixesLoaded = IsModLoaded(Const.BUTTERYFIXES_GUID);
            bool isModPeepersLoaded = IsModLoaded(Const.PEEPERS_GUID);
            bool isModHotDogModelLoaded = IsModLoaded(Const.HOTDOGMODEL_GUID);
            bool isModUsualScrapLoaded = IsModLoaded(Const.USUALSCRAP_GUID);

            // -------------------
            // Read the preloaders
            List<string> preloadersFileNames = new List<string>();
            foreach (string file in System.IO.Directory.GetFiles(Path.GetFullPath(Paths.PatcherPluginPath), "*.dll", SearchOption.AllDirectories))
            {
                preloadersFileNames.Add(Path.GetFileName(file));
            }
            // Are these preloaders loaded ?
            bool isModAdditionalNetworkingLoaded = IsPreLoaderLoaded(Const.ADDITIONALNETWORKING_DLLFILENAME, preloadersFileNames);
            IsModMonoProfilerLoaderLoaded = IsPreLoaderLoaded(Const.MONOPROFILERLOADER_DLLFILENAME, preloadersFileNames);

            // -----------------------------
            // Compatibility with other mods
            Assembly? patchesAssembly = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "LethalInternship.Patches")
                {
                    patchesAssembly = asm;
                    break;
                }
            }

            if (patchesAssembly == null)
            {
                LogError("LethalInternship.Patches not found ! Cannot apply mod patches !");
                return;
            }

            if (isModMoreEmotesLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.MoreEmotes.MoreEmotesPatch"));
            }
            if (isModBetterEmotesLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "StartPostfix"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BetterEmotes.BetterEmotesPatch"), "StartPostfix_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "UpdatePrefix"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BetterEmotes.BetterEmotesPatch"), "UpdatePrefix_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "UpdatePostfix"),
                               null,
                               null,
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BetterEmotes.BetterEmotesPatch"), "UpdatePostfix_Transpiler"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "PerformEmotePrefix"),
                               null,
                               null,
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BetterEmotes.BetterEmotesPatch"), "PerformEmotePrefix_Transpiler"));
            }
            if (IsModTooManyEmotesLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.TooManyEmotes.EmoteControllerPlayerPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.TooManyEmotes.ThirdPersonEmoteControllerPatch"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.TooManyEmotes.TooManyEmotesUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (IsModMoreCompanyLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.MoreCompany.LookForPlayersForestGiantPatchPatch"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.MoreCompany.MoreCompanyUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (IsModModelReplacementAPILoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.AvatarUpdaterPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ManagerBasePatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementPlayerControllerBPatchPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ViewModelUpdaterPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ViewStateManagerPatch"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIUtils")?.GetMethod("Init")?.Invoke(null, null);
            }

            if (isModLethalPhonesLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalPhones.PlayerPhonePatchPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalPhones.PhoneBehaviorPatch"));
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalPhones.PlayerPhonePatchLI"));
            }
            if (isModFasterItemDropshipLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.FasterItemDropship.FasterItemDropshipPatch"));
            }
            if (isModAdditionalNetworkingLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("AdditionalNetworking.Patches.Inventory.PlayerControllerBPatch"), "OnStart"),
                               null,
                               null,
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.AdditionalNetworking.AdditionalNetworkingPatch"), "Start_Transpiler"));
            }
            if (isModShowCapacityLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ShowCapacity.Patches.PlayerControllerBPatch"), "Update_PreFix"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ShowCapacity.ShowCapacityPatch"), "Update_PreFix_Prefix"));
            }
            if (IsModReviveCompanyLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ReviveCompany.ReviveCompanyGeneralUtilPatch"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("OPJosMod.ReviveCompany.Patches.PlayerControllerBPatch"), "setHoverTipAndCurrentInteractTriggerPatch"),
                               null,
                               null,
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ReviveCompany.ReviveCompanyPlayerControllerBPatchPatch"), "SetHoverTipAndCurrentInteractTriggerPatch_Transpiler"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ReviveCompany.ReviveCompanyUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (IsModBunkbedReviveLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BunkbedRevive.BunkbedControllerPatch"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.BunkbedRevive.BunkbedReviveUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (isModZaprillatorLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("Zaprillator.Behaviors.RevivablePlayer"), "IShockableWithGun.StopShockingWithGun"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.Zaprillator.RevivablePlayerPatch"), "StopShockingWithGun_Prefix"));
            }
            if (isModReservedItemSlotCoreLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ReservedItemSlotCore.Patches.PlayerPatcher"), "InitializePlayerControllerLate"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ReservedItemSlotCore.PlayerPatcherPatch"), "InitializePlayerControllerLate_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ReservedItemSlotCore.Patches.PlayerPatcher"), "CheckForChangedInventorySize"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ReservedItemSlotCore.PlayerPatcherPatch"), "CheckForChangedInventorySize_Prefix"));
            }
            if (isModLethalProgressionLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.HPRegen"), "HPRegenUpdate"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalProgression.HPRegenPatch"), "HPRegenUpdate_Prefix"));

                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "EnteredWater"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalProgression.OxygenPatch"), "EnteredWater_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "LeftWater"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalProgression.OxygenPatch"), "LeftWater_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "ShouldDrown"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalProgression.OxygenPatch"), "ShouldDrown_Prefix"));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "OxygenUpdate"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalProgression.OxygenPatch"), "OxygenUpdate_Prefix"));
            }
            if (isModQuickBuyLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("QuickBuyMenu.Plugin"), "RunQuickBuy"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.QuickBuy.QuickBuyMenuPatch"), "RunQuickBuy_Prefix"));
            }
            if (isModLCAlwaysHearWalkieModLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LCAlwaysHearWalkieMod.Patches.PlayerControllerBPatch"), "alwaysHearWalkieTalkiesPatch"),
                               null,
                               null,
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LCAlwaysHearActiveWalkie.LCAlwaysHearActiveWalkiePatch"), "alwaysHearWalkieTalkiesPatch_Transpiler"));
            }
            if (isModButteryFixesLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ButteryFixes.Patches.Player.BodyPatches"), "DeadBodyInfoPostStart"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.ButteryFixes.BodyPatchesPatch"), "DeadBodyInfoPostStart_Prefix"));
            }
            if (isModPeepersLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.Peepers.PeeperAttachHitboxPatch"));
            }
            if (isModHotDogModelLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.HotdogScout.JawMovementPatch"));
            }
            if (IsModMipaLoaded)
            {
                _harmony.PatchAll(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.Mipa.SkinApplyPatch"));

                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.Mipa.MipaUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (isModUsualScrapLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "RevivePlayer"),
                               new HarmonyMethod(patchesAssembly.GetType("LethalInternship.Patches.ModPatches.UsualScrap.DefibrillatorScriptPatch"), "RevivePlayer_Prefix"));
            }
            if (IsModLethalMinLoaded)
            {
                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.LethalMin.LethalMinUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (IsModCustomItemBehaviourLibraryLoaded)
            {
                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.CustomItemBehaviourLibrary.CustomItemBehaviourLibraryUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
            if (IsModMonoProfilerLoaderLoaded)
            {
                patchesAssembly.GetType("LethalInternship.Patches.ModPatches.MonoProfiler.MonoProfilerUtils")?.GetMethod("Init")?.Invoke(null, null);
            }
        }

        private bool IsModLoaded(string modGUID)
        {
            bool ret = Chainloader.PluginInfos.ContainsKey(modGUID);
            if (ret)
            {
                LogInfo($"Mod compatibility loaded for mod : GUID {modGUID}");
            }

            //foreach (var a in Chainloader.PluginInfos)
            //{
            //    LogDebug($"{a.Key}");
            //}

            return ret;
        }

        private bool IsPreLoaderLoaded(string dllFileName, List<string> fileNames)
        {
            bool ret = fileNames.Contains(dllFileName);
            if (ret)
            {
                LogInfo($"Mod compatibility loaded for preloader : {dllFileName}");
            }

            //foreach (var a in fileNames)
            //{
            //    LogDebug($"{a}");
            //}

            return ret;
        }


        internal void InitHooks()
        {
            PluginLoggerHook.LogDebug = LogDebug;
            PluginLoggerHook.LogInfo = LogInfo;
            PluginLoggerHook.LogWarning = LogWarning;
            PluginLoggerHook.LogError = LogError;
        }

        internal static void LogDebug(string debugLog)
        {
            if (!Plugin.Config.EnableDebugLog)
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

        internal void InitSharedValues()
        {
            InitHooks();

            PluginEventsProvider.Events = new PluginEventSource(Config);
            PluginRuntimeProvider.Context = new PluginRuntimeContext();
        }
    }

    public class PluginEventSource : IPluginRuntimeEvents
    {
        public event EventHandler? InitialSyncCompleted;

        public PluginEventSource(Configs.Config config)
        {
            config.InitialSyncCompleted += Config_InitialSyncCompleted;
        }

        private void Config_InitialSyncCompleted(object sender, EventArgs e)
        {
            InitialSyncCompleted?.Invoke(sender, e);
        }
    }

    public class PluginRuntimeContext : IPluginRuntimeContext
    {
        public string Plugin_Guid => PluginInfo.PLUGIN_GUID;
        public string Plugin_Version => PluginInfo.PLUGIN_VERSION;
        public string Plugin_Name => PluginInfo.PLUGIN_NAME;

        public string ConfigPath => Paths.ConfigPath;
        public string VoicesPath => Utility.CombinePaths(Paths.ConfigPath, PluginInfo.PLUGIN_GUID, VoicesConst.VOICES_PATH);

        public EnemyType InternNPCPrefab => Plugin.InternNPCPrefab;
        public bool UIAssetsLoaded => Plugin.UIAssetsLoaded;

        public GameObject CommandsSingleInternUIPrefab => Plugin.CommandsSingleInternUIPrefab;
        public GameObject CommandsMultipleInternsUIPrefab => Plugin.CommandsMultipleInternsUIPrefab;

        public GameObject WorldIconPrefab => Plugin.WorldIconPrefab;
        public GameObject InputIconPrefab => Plugin.InputIconPrefab;

        public GameObject DefaultIconImagePrefab => Plugin.DefaultIconImagePrefab;
        public GameObject PointerIconImagePrefab => Plugin.PointerIconImagePrefab;
        public GameObject PedestrianIconImagePrefab => Plugin.PedestrianIconImagePrefab;
        public GameObject VehicleIconImagePrefab => Plugin.VehicleIconImagePrefab;
        public GameObject ShipIconImagePrefab => Plugin.ShipIconImagePrefab;
        public GameObject MeetingPointIconImagePrefab => Plugin.MeetingPointIconImagePrefab;
        public GameObject GatheringPointIconImagePrefab => Plugin.GatheringPointIconImagePrefab;
        public GameObject AttackIconImagePrefab => Plugin.AttackIconImagePrefab;

        public string DirectoryName => Plugin.DirectoryName;
        public ILethalInternshipInputs InputActionsInstance => Plugin.InputActionsInstance;
        public IConfig Config => Plugin.Config;

        public int PluginIrlPlayersCount { get => Plugin.PluginIrlPlayersCount; set => Plugin.PluginIrlPlayersCount = value; }

        public bool IsModTooManyEmotesLoaded => Plugin.IsModTooManyEmotesLoaded;
        public bool IsModModelReplacementAPILoaded => Plugin.IsModModelReplacementAPILoaded;
        public bool IsModCustomItemBehaviourLibraryLoaded => Plugin.IsModCustomItemBehaviourLibraryLoaded;
        public bool IsModMoreCompanyLoaded => Plugin.IsModMoreCompanyLoaded;
        public bool IsModReviveCompanyLoaded => Plugin.IsModReviveCompanyLoaded;
        public bool IsModBunkbedReviveLoaded => Plugin.IsModBunkbedReviveLoaded;
        public bool IsModLethalMinLoaded => Plugin.IsModLethalMinLoaded;
        public bool IsModMipaLoaded => Plugin.IsModMipaLoaded;
        public bool IsModMonoProfilerLoaderLoaded => Plugin.IsModMonoProfilerLoaderLoaded;
    }
}