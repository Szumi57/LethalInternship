﻿using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using LethalInternship.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        [SyncedEntryField] public SyncedEntry<int> OptionInternNames;
        [SyncedEntryField] public SyncedEntry<string> ListUserCustomNames;
        [SyncedEntryField] public SyncedEntry<bool> UseCustomNamesRandomly;
                           
        // Behaviour       
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

        // Debug
        public ConfigEntry<bool> EnableDebugLog;

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
            OptionInternNames = cfg.BindSyncedEntry(Const.ConfigSectionNames,
                                         "Option for custom names",
                                         defaultValue: (int)Const.DEFAULT_CONFIG_ENUM_INTERN_NAMES,
                                         new ConfigDescription("0: default names \"Intern #(number)\" | 1: default custom names list used by the mod | 2: user defined custom names list",
                                                               new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumOptionInternNames)).Cast<int>().Min(),
                                                                                             Enum.GetValues(typeof(EnumOptionInternNames)).Cast<int>().Max())));

            ListUserCustomNames = cfg.BindSyncedEntry(Const.ConfigSectionNames,
                                       "List of user custom names for interns, to use with option 2 : user defined custom names list",
                                       defaultVal: String.Join(", ", Const.DEFAULT_LIST_CUSTOM_INTERN_NAMES),
                                       "Write your own list of names like : name surname, name surname, etc... (needs option 2 : user defined custom names list)");

            UseCustomNamesRandomly = cfg.BindSyncedEntry(Const.ConfigSectionNames,
                                              "Randomness of custom names",
                                              defaultVal: true,
                                              "Use the list of custom names randomly ?");

            // Behaviour
            ChangeSuitBehaviour = cfg.BindSyncedEntry(Const.ConfigSectionBehaviour,
                                               "Options for changing interns suits",
                                               defaultValue: (int)Const.DEFAULT_CONFIG_ENUM_INTERN_SUIT_CHANGE,
                                               new ConfigDescription("0: Change manually | 1: Automatically change with the same suit as player | 2: Random available suit when the intern spawn",
                                                               new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumOptionInternSuitChange)).Cast<int>().Min(),
                                                                                             Enum.GetValues(typeof(EnumOptionInternSuitChange)).Cast<int>().Max())));

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

            // Debug
            EnableDebugLog = cfg.Bind(Const.ConfigSectionDebug,
                                      "EnableDebugLog",
                                      defaultValue: false,
                                      "Enable the debug logs used for this mod.");

            ClearUnusedEntries(cfg);
            cfg.SaveOnConfigSet = true;

            ConfigManager.Register(this);
        }

        public EnumOptionInternNames GetOptionInternNames()
        {
            if (!Enum.IsDefined(typeof(EnumOptionInternNames), OptionInternNames.Value))
            {
                Plugin.LogWarning($"Could not get option for intern names in config, value {OptionInternNames.Value}");
                return EnumOptionInternNames.Default;
            }

            return (EnumOptionInternNames)OptionInternNames.Value;
        }

        public string[] GetArrayOfUserCustomNames()
        {
            if (string.IsNullOrWhiteSpace(ListUserCustomNames.Value))
            {
                return new string[] { };
            }

            string[] arrayOfNames = ListUserCustomNames.Value.Split(new[] { ',', ';' });
            for (int i = 0; i < arrayOfNames.Length; i++)
            {
                arrayOfNames[i] = arrayOfNames[i].Trim();
            }

            return arrayOfNames;
        }

        public string GetTitleInternshipProgram()
        {
            return string.Format(Const.STRING_INTERNSHIP_PROGRAM_HELP, TitleInHelpMenu.Value.ToUpper(), SubTitleInHelpMenu.Value);
        }

        private void ClearUnusedEntries(ConfigFile cfg)
        {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            cfg.Save(); // Save the config file to save these changes
        }
    }
}