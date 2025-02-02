﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Inputs;
using LethalInternship.Managers;
using LethalInternship.Patches.EnemiesPatches;
using LethalInternship.Patches.GameEnginePatches;
using LethalInternship.Patches.MapHazardsPatches;
using LethalInternship.Patches.MapPatches;
using LethalInternship.Patches.ModPatches.AdditionalNetworking;
using LethalInternship.Patches.ModPatches.BetterEmotes;
using LethalInternship.Patches.ModPatches.BunkbedRevive;
using LethalInternship.Patches.ModPatches.ButteryFixes;
using LethalInternship.Patches.ModPatches.FasterItemDropship;
using LethalInternship.Patches.ModPatches.HotdogScout;
using LethalInternship.Patches.ModPatches.LCAlwaysHearActiveWalkie;
using LethalInternship.Patches.ModPatches.LethalPhones;
using LethalInternship.Patches.ModPatches.LethalProgression;
using LethalInternship.Patches.ModPatches.Mipa;
using LethalInternship.Patches.ModPatches.ModelRplcmntAPI;
using LethalInternship.Patches.ModPatches.MoreCompany;
using LethalInternship.Patches.ModPatches.MoreEmotes;
using LethalInternship.Patches.ModPatches.Peepers;
using LethalInternship.Patches.ModPatches.QuickBuy;
using LethalInternship.Patches.ModPatches.ReservedItemSlotCore;
using LethalInternship.Patches.ModPatches.ReviveCompany;
using LethalInternship.Patches.ModPatches.ShowCapacity;
using LethalInternship.Patches.ModPatches.TooManyEmotes;
using LethalInternship.Patches.ModPatches.Zaprillator;
using LethalInternship.Patches.NpcPatches;
using LethalInternship.Patches.ObjectsPatches;
using LethalInternship.Patches.TerminalPatches;
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
        internal static string DirectoryName = null!;

        internal static EnemyType InternNPCPrefab = null!;
        internal static int PluginIrlPlayersCount = 0;

        internal static new ManualLogSource Logger = null!;
        internal static new Configs.Config Config = null!;
        internal static LethalInternshipInputs InputActionsInstance = null!;

        internal static bool IsModTooManyEmotesLoaded = false;
        internal static bool IsModModelReplacementAPILoaded = false;
        internal static bool IsModCustomItemBehaviourLibraryLoaded = false;
        internal static bool IsModMoreCompanyLoaded = false;
        internal static bool IsModReviveCompanyLoaded = false;
        internal static bool IsModBunkbedReviveLoaded = false;
        internal static bool IsModLethalMinLoaded = false;
        internal static bool IsModMipaLoaded = false;

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
                                                                                            && x.name != "Collision"
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
            _harmony.PatchAll(typeof(AudioMixerPatch));
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
            _harmony.PatchAll(typeof(CaveDwellerAIPatch));
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
            _harmony.PatchAll(typeof(ManualCameraRendererPatch));
            _harmony.PatchAll(typeof(ShipTeleporterPatch));
            _harmony.PatchAll(typeof(VehicleControllerPatch));

            // Objects
            _harmony.PatchAll(typeof(DeadBodyInfoPatch));
            _harmony.PatchAll(typeof(GrabbableObjectPatch));
            _harmony.PatchAll(typeof(RagdollGrabbableObjectPatch));
            _harmony.PatchAll(typeof(ShotgunItemPatch));
            _harmony.PatchAll(typeof(StunGrenadeItemPatch));

            // Terminal
            _harmony.PatchAll(typeof(TerminalPatch));
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

            // -------------------
            // Read the preloaders
            List<string> preloadersFileNames = new List<string>();
            foreach (string file in System.IO.Directory.GetFiles(Path.GetFullPath(Paths.PatcherPluginPath), "*.dll", SearchOption.AllDirectories))
            {
                preloadersFileNames.Add(Path.GetFileName(file));
            }
            // Are these preloaders loaded ?
            bool isModAdditionalNetworkingLoaded = IsPreLoaderLoaded(Const.ADDITIONALNETWORKING_DLLFILENAME, preloadersFileNames);

            // -----------------------------
            // Compatibility with other mods
            if (isModMoreEmotesLoaded)
            {
                _harmony.PatchAll(typeof(MoreEmotesPatch));
            }
            if (isModBetterEmotesLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "StartPostfix"),
                               new HarmonyMethod(typeof(BetterEmotesPatch), nameof(BetterEmotesPatch.StartPostfix_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "UpdatePrefix"),
                               new HarmonyMethod(typeof(BetterEmotesPatch), nameof(BetterEmotesPatch.UpdatePrefix_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "UpdatePostfix"),
                               null,
                               null,
                               new HarmonyMethod(typeof(BetterEmotesPatch), nameof(BetterEmotesPatch.UpdatePostfix_Transpiler)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("BetterEmote.Patches.EmotePatch"), "PerformEmotePrefix"),
                               null,
                               null,
                               new HarmonyMethod(typeof(BetterEmotesPatch), nameof(BetterEmotesPatch.PerformEmotePrefix_Transpiler)));
            }
            if (IsModTooManyEmotesLoaded)
            {
                _harmony.PatchAll(typeof(EmoteControllerPlayerPatch));
                _harmony.PatchAll(typeof(ThirdPersonEmoteControllerPatch));
            }
            if (IsModMoreCompanyLoaded)
            {
                _harmony.PatchAll(typeof(LookForPlayersForestGiantPatchPatch));
            }
            if (IsModModelReplacementAPILoaded)
            {
                _harmony.PatchAll(typeof(BodyReplacementBasePatch));
                _harmony.PatchAll(typeof(ModelReplacementPlayerControllerBPatchPatch));
                _harmony.PatchAll(typeof(ModelReplacementAPIPatch));
                _harmony.PatchAll(typeof(ManagerBasePatch));
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
            if (IsModReviveCompanyLoaded)
            {
                _harmony.PatchAll(typeof(ReviveCompanyGeneralUtilPatch));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("OPJosMod.ReviveCompany.Patches.PlayerControllerBPatch"), "setHoverTipAndCurrentInteractTriggerPatch"),
                               null,
                               null,
                               new HarmonyMethod(typeof(ReviveCompanyPlayerControllerBPatchPatch), nameof(ReviveCompanyPlayerControllerBPatchPatch.SetHoverTipAndCurrentInteractTriggerPatch_Transpiler)));
            }
            if (IsModBunkbedReviveLoaded)
            {
                _harmony.PatchAll(typeof(BunkbedControllerPatch));
            }
            if (isModZaprillatorLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("Zaprillator.Behaviors.RevivablePlayer"), "IShockableWithGun.StopShockingWithGun"),
                               new HarmonyMethod(typeof(RevivablePlayerPatch), nameof(RevivablePlayerPatch.StopShockingWithGun_Prefix)));
            }
            if (isModReservedItemSlotCoreLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ReservedItemSlotCore.Patches.PlayerPatcher"), "InitializePlayerControllerLate"),
                               new HarmonyMethod(typeof(PlayerPatcherPatch), nameof(PlayerPatcherPatch.InitializePlayerControllerLate_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ReservedItemSlotCore.Patches.PlayerPatcher"), "CheckForChangedInventorySize"),
                               new HarmonyMethod(typeof(PlayerPatcherPatch), nameof(PlayerPatcherPatch.CheckForChangedInventorySize_Prefix)));
            }
            if (isModLethalProgressionLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.HPRegen"), "HPRegenUpdate"),
                               new HarmonyMethod(typeof(HPRegenPatch), nameof(HPRegenPatch.HPRegenUpdate_Prefix)));

                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "EnteredWater"),
                               new HarmonyMethod(typeof(OxygenPatch), nameof(OxygenPatch.EnteredWater_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "LeftWater"),
                               new HarmonyMethod(typeof(OxygenPatch), nameof(OxygenPatch.LeftWater_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "ShouldDrown"),
                               new HarmonyMethod(typeof(OxygenPatch), nameof(OxygenPatch.ShouldDrown_Prefix)));
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LethalProgression.Skills.Oxygen"), "OxygenUpdate"),
                               new HarmonyMethod(typeof(OxygenPatch), nameof(OxygenPatch.OxygenUpdate_Prefix)));
            }
            if (isModQuickBuyLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("QuickBuyMenu.Plugin"), "RunQuickBuy"),
                               new HarmonyMethod(typeof(QuickBuyMenuPatch), nameof(QuickBuyMenuPatch.RunQuickBuy_Prefix)));
            }
            if (isModLCAlwaysHearWalkieModLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LCAlwaysHearWalkieMod.Patches.PlayerControllerBPatch"), "alwaysHearWalkieTalkiesPatch"),
                               null,
                               null,
                               new HarmonyMethod(typeof(LCAlwaysHearActiveWalkiePatch), nameof(LCAlwaysHearActiveWalkiePatch.alwaysHearWalkieTalkiesPatch_Transpiler)));
            }
            if (isModButteryFixesLoaded)
            {
                _harmony.Patch(AccessTools.Method(AccessTools.TypeByName("ButteryFixes.Patches.Player.BodyPatches"), "DeadBodyInfoPostStart"),
                               new HarmonyMethod(typeof(BodyPatchesPatch), nameof(BodyPatchesPatch.DeadBodyInfoPostStart_Prefix)));
            }
            if (isModPeepersLoaded)
            {
                _harmony.PatchAll(typeof(PeeperAttachHitboxPatch));
            }
            if (isModHotDogModelLoaded)
            {
                _harmony.PatchAll(typeof(JawMovementPatch));
            }
            if (IsModMipaLoaded)
            {
                _harmony.PatchAll(typeof(SkinApplyPatch));
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

        private static void InitPluginManager()
        {
            GameObject gameObject = new GameObject("PluginManager");
            gameObject.AddComponent<PluginManager>();
            PluginManager.Instance.InitManagers();
        }

        internal static void LogDebug(string debugLog)
        {
            if (!Plugin.Config.EnableDebugLog.Value)
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