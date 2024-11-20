using BepInEx;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalInternship.Configs
{
    // For more info on custom configs, see https://lethal.wiki/dev/intermediate/custom-configs
    // Csync https://lethal.wiki/dev/apis/csync/usage-guide

    /// <summary>
    /// Config class, manage parameters editable by the player (irl)
    /// </summary>
    public class Config : SyncedConfig2<Config>
    {
        // Internship program
        [SyncedEntryField] public SyncedEntry<int> MaxInternsAvailable;
        [SyncedEntryField] public SyncedEntry<int> InternPrice;
        [SyncedEntryField] public SyncedEntry<int> InternMaxHealth;
        [SyncedEntryField] public SyncedEntry<float> InternSizeScale;

        [SyncedEntryField] public SyncedEntry<string> TitleInHelpMenu;
        [SyncedEntryField] public SyncedEntry<string> SubTitleInHelpMenu;

        [SyncedEntryField] public SyncedEntry<bool> CanSpectateInterns;
        [SyncedEntryField] public SyncedEntry<bool> RadarEnabled;

        // Interns names   
        [SyncedEntryField] public SyncedEntry<bool> SpawnIdentitiesRandomly;

        // Behaviour       
        [SyncedEntryField] public SyncedEntry<bool> FollowCrouchWithPlayer;
        [SyncedEntryField] public SyncedEntry<int> ChangeSuitBehaviour;
        [SyncedEntryField] public SyncedEntry<bool> TeleportWhenUsingLadders;
        [SyncedEntryField] public SyncedEntry<bool> GrabItemsNearEntrances;
        [SyncedEntryField] public SyncedEntry<bool> GrabBeesNest;
        [SyncedEntryField] public SyncedEntry<bool> GrabDeadBodies;
        [SyncedEntryField] public SyncedEntry<bool> GrabManeaterBaby;
        [SyncedEntryField] public SyncedEntry<bool> GrabWheelbarrow;
        [SyncedEntryField] public SyncedEntry<bool> GrabShoppingCart;

        // Teleporters
        [SyncedEntryField] public SyncedEntry<bool> TeleportedInternDropItems;

        // Voices
        [SyncedEntryField] public SyncedEntry<bool> AllowSwearing;

        // Debug
        public ConfigEntry<bool> EnableDebugLog;

        // Config identities
        public ConfigIdentities ConfigIdentities;

        public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
        {
            cfg.SaveOnConfigSet = false;

            // Internship program
            MaxInternsAvailable = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                           "Max amount of interns purchasable",
                                           defaultValue: Const.DEFAULT_MAX_INTERNS_AVAILABLE,
                                           new ConfigDescription("Be aware of possible performance problems when more than ~16 interns spawned",
                                                                 new AcceptableValueRange<int>(Const.MIN_INTERNS_AVAILABLE, Const.MAX_INTERNS_AVAILABLE)));

            InternPrice = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                   "Price",
                                   defaultValue: Const.DEFAULT_PRICE_INTERN,
                                   new ConfigDescription("Price for one intern",
                                                         new AcceptableValueRange<int>(Const.MIN_PRICE_INTERN, Const.MAX_PRICE_INTERN)));

            InternMaxHealth = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                       "Max health",
                                       defaultValue: Const.DEFAULT_INTERN_MAX_HEALTH,
                                       new ConfigDescription("Max health of intern",
                                                             new AcceptableValueRange<int>(Const.MIN_INTERN_MAX_HEALTH, Const.MAX_INTERN_MAX_HEALTH)));

            InternSizeScale = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                       "Size multiplier of intern",
                                       defaultValue: Const.DEFAULT_SIZE_SCALE_INTERN,
                                       new ConfigDescription("Shrink (less than 1) or equals to default (=1) size of interns",
                                                             new AcceptableValueRange<float>(Const.MIN_SIZE_SCALE_INTERN, Const.MAX_SIZE_SCALE_INTERN)));

            CanSpectateInterns = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                                     "Spectate interns",
                                                     defaultVal: false,
                                                     "Can a dead player spectate interns ?");

            RadarEnabled = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                              "Radar view enabled for interns",
                                              defaultVal: false,
                                              "Can you view the intern on the ship radar computer screen ?");

            TitleInHelpMenu = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                       "Title visible in help menu in the terminal",
                                       defaultVal: Const.DEFAULT_STRING_INTERNSHIP_PROGRAM_TITLE,
                                       "Careful ! This title will become your command to type in to enter the intership program");

            SubTitleInHelpMenu = cfg.BindSyncedEntry(Const.ConfigSectionMain,
                                       "Subtitle visible in help menu under the title of intership program",
                                       defaultVal: Const.DEFAULT_STRING_INTERNSHIP_PROGRAM_SUBTITLE,
                                       "");

            // Names
            SpawnIdentitiesRandomly = cfg.BindSyncedEntry(Const.ConfigSectionIdentities,
                                              "Randomness of identities",
                                              defaultVal: true,
                                              "Spawn the interns with random identities ?");

            // Behaviour
            FollowCrouchWithPlayer = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                               "Crouch with player",
                                               defaultVal: true,
                                               "Should the intern crouch like the player is crouching ?");

            ChangeSuitBehaviour = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                               "Options for changing interns suits",
                                               defaultValue: (int)Const.DEFAULT_CONFIG_ENUM_INTERN_SUIT_CHANGE,
                                               new ConfigDescription("0: Change manually | 1: Automatically change with the same suit as player | 2: Random available suit when the intern spawn",
                                                               new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumOptionSuitChange)).Cast<int>().Min(),
                                                                                             Enum.GetValues(typeof(EnumOptionSuitChange)).Cast<int>().Max())));

            TeleportWhenUsingLadders = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                               "Teleport when using ladders",
                                               defaultVal: false,
                                               "Should the intern just teleport and bypass any animations when using ladders ?");

            GrabItemsNearEntrances = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                               "Grab items near entrances",
                                               defaultVal: true,
                                               "Should the intern grab the items near main entrance and fire exits ?");

            GrabBeesNest = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                    "Grab bees nests",
                                    defaultVal: false,
                                    "Should the intern try to grab bees nests ?");

            GrabDeadBodies = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                      "Grab dead bodies",
                                      defaultVal: false,
                                      "Should the intern try to grab dead bodies ?");

            GrabManeaterBaby = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                      "Grab the baby maneater",
                                      defaultVal: false,
                                      "Should the intern try to grab the baby maneater ?");

            GrabWheelbarrow = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                      "Grab the wheelbarrow",
                                      defaultVal: false,
                                      "Should the intern try to grab the wheelbarrow (mod) ?");

            GrabShoppingCart = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                      "Grab the shppping cart",
                                      defaultVal: false,
                                      "Should the intern try to grab the shopping cart (mod) ?");

            // Teleporters
            TeleportedInternDropItems = cfg.BindSyncedEntry(Const.ConfigSectionTeleporters,
                                                            "Teleported intern drop item (not if the intern is grabbed by player)",
                                                            defaultVal: true,
                                                            "Should the intern his held item before teleporting ?");

            // Voices
            AllowSwearing = cfg.BindSyncedEntry(Const.ConfigSectionVoices,
                                                "Swear words",
                                                defaultVal: false,
                                                "Allow the use of swear words in interns voice lines ?");

            // Debug
            EnableDebugLog = cfg.Bind(Const.ConfigSectionDebug,
                                      "EnableDebugLog",
                                      defaultValue: false,
                                      "Enable the debug logs used for this mod.");

            ClearUnusedEntries(cfg);
            cfg.SaveOnConfigSet = true;

            // Config identities
            CopyDefaultConfigIdentitiesJson();
            ReadAndLoadConfigIdentitiesFromUser();

            ConfigManager.Register(this);
        }

        private void LogDebugInConfig(string debugLog)
        {
            if (!EnableDebugLog.Value)
            {
                return;
            }
            Plugin.Logger.LogDebug(debugLog);
        }

        public string GetTitleInternshipProgram()
        {
            return string.Format(TerminalConst.STRING_INTERNSHIP_PROGRAM_HELP, TitleInHelpMenu.Value.ToUpper(), SubTitleInHelpMenu.Value);
        }

        private void ClearUnusedEntries(ConfigFile cfg)
        {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            cfg.Save(); // Save the config file to save these changes
        }

        private void CopyDefaultConfigIdentitiesJson()
        {
            string directoryPath = Utility.CombinePaths(Paths.ConfigPath, PluginInfo.PLUGIN_GUID);
            Directory.CreateDirectory(directoryPath);

            string json = ReadJsonResource("LethalInternship.Configs.ConfigIdentities.json");
            using (StreamWriter outputFile = new StreamWriter(Utility.CombinePaths(directoryPath, Const.FILE_NAME_CONFIG_IDENTITIES_DEFAULT)))
            {
                outputFile.WriteLine(json);
            }
        }

        private string ReadJsonResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void ReadAndLoadConfigIdentitiesFromUser()
        {
            string json;

            // Try to read user config file
            string path = Utility.CombinePaths(Paths.ConfigPath, PluginInfo.PLUGIN_GUID, Const.FILE_NAME_CONFIG_IDENTITIES_USER);
            if (File.Exists(path))
            {
                using (StreamReader r = new StreamReader(path))
                {
                    json = r.ReadToEnd();
                }
            }
            else
            {
                path = "LethalInternship.Configs.ConfigIdentities.json";
                json = ReadJsonResource(path);
            }

            ConfigIdentities = JsonUtility.FromJson<ConfigIdentities>(json);

            LogDebugInConfig($"Loaded {ConfigIdentities.configIdentities.Length} identities from file : {path}");
            foreach (ConfigIdentity configIdentity in ConfigIdentities.configIdentities)
            {
                LogDebugInConfig($"{configIdentity.ToString()}");
            }
        }
    }
}