using BepInEx;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public ConfigEntry<int> MaxAnimatedInterns;

        [SyncedEntryField] public SyncedEntry<string> TitleInHelpMenu;
        [SyncedEntryField] public SyncedEntry<string> SubTitleInHelpMenu;

        [SyncedEntryField] public SyncedEntry<bool> CanSpectateInterns;
        [SyncedEntryField] public SyncedEntry<bool> RadarEnabled;

        // Identity  
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
        public ConfigEntry<string> VolumeMultiplierInterns;
        public ConfigEntry<int> Talkativeness;
        public ConfigEntry<bool> AllowSwearing;

        // Debug
        public ConfigEntry<bool> EnableDebugLog;

        // Config identities
        public ConfigIdentities ConfigIdentities;

        public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
        {
            cfg.SaveOnConfigSet = false;

            // Internship program
            MaxInternsAvailable = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                           "Max amount of interns purchasable",
                                           defaultValue: ConfigConst.DEFAULT_MAX_INTERNS_AVAILABLE,
                                           new ConfigDescription("Be aware of possible performance problems when more than ~16 interns spawned",
                                                                 new AcceptableValueRange<int>(ConfigConst.MIN_INTERNS_AVAILABLE, ConfigConst.MAX_INTERNS_AVAILABLE)));

            InternPrice = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                   "Price",
                                   defaultValue: ConfigConst.DEFAULT_PRICE_INTERN,
                                   new ConfigDescription("Price for one intern",
                                                         new AcceptableValueRange<int>(ConfigConst.MIN_PRICE_INTERN, ConfigConst.MAX_PRICE_INTERN)));

            InternMaxHealth = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                       "Max health",
                                       defaultValue: ConfigConst.DEFAULT_INTERN_MAX_HEALTH,
                                       new ConfigDescription("Max health of intern",
                                                             new AcceptableValueRange<int>(ConfigConst.MIN_INTERN_MAX_HEALTH, ConfigConst.MAX_INTERN_MAX_HEALTH)));

            InternSizeScale = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                       "Size multiplier of intern",
                                       defaultValue: ConfigConst.DEFAULT_SIZE_SCALE_INTERN,
                                       new ConfigDescription("Shrink (less than 1) or equals to default (=1) size of interns",
                                                             new AcceptableValueRange<float>(ConfigConst.MIN_SIZE_SCALE_INTERN, ConfigConst.MAX_SIZE_SCALE_INTERN)));

            MaxAnimatedInterns = cfg.Bind(ConfigConst.ConfigSectionMain,
                                   "Max animated intern at once",
                                   defaultValue: ConfigConst.MAX_INTERNS_AVAILABLE,
                                   new ConfigDescription("Set the maximum of interns that can be animated at the same time (if heavy lag occurs when looking at a lot of interns) (client only)",
                                                         new AcceptableValueRange<int>(1, ConfigConst.MAX_INTERNS_AVAILABLE)));

            CanSpectateInterns = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                                     "Spectate interns",
                                                     defaultVal: false,
                                                     "Can a dead player spectate interns ?");

            RadarEnabled = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                              "Radar monitoring enabled for interns",
                                              defaultVal: false,
                                              "Can you monitor the intern on the ship radar computer screen ?");

            TitleInHelpMenu = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                       "Title visible in help menu in the terminal",
                                       defaultVal: ConfigConst.DEFAULT_STRING_INTERNSHIP_PROGRAM_TITLE,
                                       "Careful ! This title will become your command to type in to enter the intership program");

            SubTitleInHelpMenu = cfg.BindSyncedEntry(ConfigConst.ConfigSectionMain,
                                       "Subtitle visible in help menu under the title of intership program",
                                       defaultVal: ConfigConst.DEFAULT_STRING_INTERNSHIP_PROGRAM_SUBTITLE,
                                       "");

            // Identities
            SpawnIdentitiesRandomly = cfg.BindSyncedEntry(ConfigConst.ConfigSectionIdentities,
                                              "Randomness of identities",
                                              defaultVal: true,
                                              "Spawn the interns with random identities ?");

            // Behaviour
            FollowCrouchWithPlayer = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                               "Crouch with player",
                                               defaultVal: true,
                                               "Should the intern crouch like the player is crouching ?");

            ChangeSuitBehaviour = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                               "Options for changing interns suits",
                                               defaultValue: (int)ConfigConst.DEFAULT_CONFIG_ENUM_INTERN_SUIT_CHANGE,
                                               new ConfigDescription("0: Change manually | 1: Automatically change with the same suit as player | 2: Random available suit when the intern spawn",
                                                               new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumOptionSuitChange)).Cast<int>().Min(),
                                                                                             Enum.GetValues(typeof(EnumOptionSuitChange)).Cast<int>().Max())));

            TeleportWhenUsingLadders = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                               "Teleport when using ladders",
                                               defaultVal: false,
                                               "Should the intern just teleport and bypass any animations when using ladders ?");

            GrabItemsNearEntrances = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                               "Grab items near entrances",
                                               defaultVal: true,
                                               "Should the intern grab the items near main entrance and fire exits ?");

            GrabBeesNest = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                    "Grab bees nests",
                                    defaultVal: false,
                                    "Should the intern try to grab bees nests ?");

            GrabDeadBodies = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                      "Grab dead bodies",
                                      defaultVal: false,
                                      "Should the intern try to grab dead bodies ?");

            GrabManeaterBaby = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                      "Grab the baby maneater",
                                      defaultVal: false,
                                      "Should the intern try to grab the baby maneater ?");

            GrabWheelbarrow = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                      "Grab the wheelbarrow",
                                      defaultVal: false,
                                      "Should the intern try to grab the wheelbarrow (mod) ?");

            GrabShoppingCart = cfg.BindSyncedEntry(ConfigConst.ConfigSectionBehaviour,
                                      "Grab the shppping cart",
                                      defaultVal: false,
                                      "Should the intern try to grab the shopping cart (mod) ?");

            // Teleporters
            TeleportedInternDropItems = cfg.BindSyncedEntry(ConfigConst.ConfigSectionTeleporters,
                                                            "Teleported intern drop item (not if the intern is grabbed by player)",
                                                            defaultVal: true,
                                                            "Should the intern his held item before teleporting ?");

            // Voices
            VolumeMultiplierInterns = cfg.Bind(ConfigConst.ConfigSectionVoices,
                                     "Volume multiplier (Client only)",
                                     defaultValue: VoicesConst.DEFAULT_VOLUME.ToString(),
                                     "Volume multiplier of voices of interns");

            Talkativeness = cfg.Bind(ConfigConst.ConfigSectionVoices,
                                     "Talkativeness (Client only)",
                                     defaultValue: (int)VoicesConst.DEFAULT_CONFIG_ENUM_TALKATIVENESS,
                                     new ConfigDescription("0: No talking | 1: Shy | 2: Normal | 3: Talkative | 4: Can't stop talking",
                                                     new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumTalkativeness)).Cast<int>().Min(),
                                                                                   Enum.GetValues(typeof(EnumTalkativeness)).Cast<int>().Max())));

            AllowSwearing = cfg.Bind(ConfigConst.ConfigSectionVoices,
                                     "Swear words (Client only)",
                                     defaultValue: false,
                                     "Allow the use of swear words in interns voice lines ?");

            // Debug
            EnableDebugLog = cfg.Bind(ConfigConst.ConfigSectionDebug,
                                      "EnableDebugLog  (Client only)",
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

        public float GetVolumeMultiplierInterns()
        {
            // https://stackoverflow.com/questions/29452263/make-tryparse-compatible-with-comma-or-dot-decimal-separator
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ",";

            if (float.TryParse(VolumeMultiplierInterns.Value, NumberStyles.Any, nfi, out float volume))
            {
                return Mathf.Clamp(volume, 0f, 1f);
            }
            return VoicesConst.DEFAULT_VOLUME;
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
            try
            {
                string directoryPath = Utility.CombinePaths(Paths.ConfigPath, PluginInfo.PLUGIN_GUID);
                Directory.CreateDirectory(directoryPath);

                string json = ReadJsonResource("LethalInternship.Configs.ConfigIdentities.json");
                using (StreamWriter outputFile = new StreamWriter(Utility.CombinePaths(directoryPath, ConfigConst.FILE_NAME_CONFIG_IDENTITIES_DEFAULT)))
                {
                    outputFile.WriteLine(json);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error while CopyDefaultConfigIdentitiesJson ! {ex}");
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
            string path = "No path yet";

            try
            {
                path = Utility.CombinePaths(Paths.ConfigPath, PluginInfo.PLUGIN_GUID, ConfigConst.FILE_NAME_CONFIG_IDENTITIES_USER);
                // Try to read user config file
                if (File.Exists(path))
                {
                    Plugin.Logger.LogInfo("User identities file found ! Reading...");
                    using (StreamReader r = new StreamReader(path))
                    {
                        json = r.ReadToEnd();
                    }

                    ConfigIdentities = JsonUtility.FromJson<ConfigIdentities>(json);
                    if (ConfigIdentities.configIdentities == null)
                    {
                        Plugin.Logger.LogWarning($"Failed to read identities from file at {path}");
                    }
                }
                else
                {
                    Plugin.Logger.LogInfo("No user identities file found. Reading default identities...");
                    path = "LethalInternship.Configs.ConfigIdentities.json";
                    json = ReadJsonResource(path);
                    ConfigIdentities = JsonUtility.FromJson<ConfigIdentities>(json);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"Error while ReadAndLoadConfigIdentitiesFromUser ! {e}");
                json = "No json, see exception above.";
            }

            if (ConfigIdentities.configIdentities == null)
            {
                Plugin.Logger.LogWarning($"A problem occured while retrieving identities from config file ! continuing with no identities... json used : \n{json}");
                ConfigIdentities = new ConfigIdentities() { configIdentities = new ConfigIdentity[0] };
            }
            else
            {
                Plugin.Logger.LogInfo($"Loaded {ConfigIdentities.configIdentities.Length} identities from file : {path}");
                foreach (ConfigIdentity configIdentity in ConfigIdentities.configIdentities)
                {
                    LogDebugInConfig($"{configIdentity.ToString()}");
                }
            }
        }
    }
}