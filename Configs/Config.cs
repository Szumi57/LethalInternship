using BepInEx.Configuration;
using LethalInternship.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LethalInternship.Configs
{
    /// <summary>
    /// Config class, manage parameters editable by the player (irl)
    /// </summary>
    public class Config
    {
        // For more info on custom configs, see https://lethal.wiki/dev/intermediate/custom-configs

        // Internship program
        public ConfigEntry<int> MaxInternsAvailable;
        public ConfigEntry<int> InternPrice;
        public ConfigEntry<int> InternMaxHealth;
        public ConfigEntry<float> InternSizeScale;

        // Interns names
        public ConfigEntry<int> OptionInternNames;
        public ConfigEntry<string> ListUserCustomNames;
        public ConfigEntry<bool> UseCustomNamesRandomly;

        // Movements
        public ConfigEntry<bool> TeleportWhenUsingLadders;

        // Debug
        public ConfigEntry<bool> EnableDebugLog;
        public ConfigEntry<bool> EnableStackTraceInDebugLog;

        public Config(ConfigFile cfg)
        {
            cfg.SaveOnConfigSet = false;

            // Internship program
            MaxInternsAvailable = cfg.Bind(Const.ConfigSectionMain,
                                           "Max amount of interns purchasable",
                                           defaultValue: Const.DEFAULT_MAX_INTERNS_AVAILABLE,
                                           new ConfigDescription("Be aware of possible performance problems when more than ~16 interns spawned",
                                                                 new AcceptableValueRange<int>(Const.MIN_INTERNS_AVAILABLE, Const.MAX_INTERNS_AVAILABLE)));

            InternPrice = cfg.Bind(Const.ConfigSectionMain,
                                   "Price",
                                   defaultValue: Const.DEFAULT_PRICE_INTERN,
                                   new ConfigDescription("Price for one intern",
                                                         new AcceptableValueRange<int>(Const.MIN_PRICE_INTERN, Const.MAX_PRICE_INTERN)));

            InternMaxHealth = cfg.Bind(Const.ConfigSectionMain,
                                       "Max health",
                                       defaultValue: Const.DEFAULT_INTERN_MAX_HEALTH,
                                       new ConfigDescription("Max health of intern",
                                                             new AcceptableValueRange<int>(Const.MIN_INTERN_MAX_HEALTH, Const.MAX_INTERN_MAX_HEALTH)));

            InternSizeScale = cfg.Bind(Const.ConfigSectionMain,
                                       "Size multiplier of intern",
                                       defaultValue: Const.DEFAULT_SIZE_SCALE_INTERN,
                                       new ConfigDescription("Shrink (less than 1) or increase (more than 1) size of interns",
                                                             new AcceptableValueRange<float>(Const.MIN_SIZE_SCALE_INTERN, Const.MAX_SIZE_SCALE_INTERN)));

            // Names
            OptionInternNames = cfg.Bind(Const.ConfigSectionNames,
                                         "Option for custom names",
                                         defaultValue: (int)Const.DEFAULT_CONFIG_ENUM_INTERN_NAMES,
                                         new ConfigDescription("0: default names \"Intern #(number)\" | 1: default custom names list used by the mod | 2: user defined custom names list",
                                                               new AcceptableValueRange<int>(Enum.GetValues(typeof(EnumOptionInternNames)).Cast<int>().Min(),
                                                                                             Enum.GetValues(typeof(EnumOptionInternNames)).Cast<int>().Max())));

            ListUserCustomNames = cfg.Bind(Const.ConfigSectionNames,
                                       "List of user custom names for interns",
                                       defaultValue: string.Empty,
                                       "Write your own list of names like : name surname, name surname, etc... (needs list of user custom names to be chosen");

            UseCustomNamesRandomly = cfg.Bind(Const.ConfigSectionNames,
                                              "Randomness of custom names",
                                              defaultValue: true,
                                              "Use the list of custom names randomly ?");

            // Movements
            TeleportWhenUsingLadders = cfg.Bind(Const.ConfigSectionMovements,
                                               "Teleport when using ladders",
                                               defaultValue: false,
                                               "Should the intern just teleport and bypass any animation when using ladders ?");

            // Debug
            EnableDebugLog = cfg.Bind(Const.ConfigSectionDebug,
                                      "EnableDebugLog",
                                      defaultValue: false,
                                      "Enable the debug logs used for this mod.");
            EnableStackTraceInDebugLog = cfg.Bind(Const.ConfigSectionDebug,
                                                  "EnableStackTraceInDebugLog",
                                                  defaultValue: false,
                                                  "Enable printing the stack trace in the error logs when using this mod.");

            ClearUnusedEntries(cfg);
            cfg.SaveOnConfigSet = true;
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

            return ListUserCustomNames.Value.Split(new[] { ',', ';' });
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