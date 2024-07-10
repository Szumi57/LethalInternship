using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace LethalInternship
{
    /// <summary>
    /// Config class, manage parameters editable by the player (irl)
    /// </summary>
    public class Config
    {
        // For more info on custom configs, see https://lethal.wiki/dev/intermediate/custom-configs
        public ConfigEntry<bool> EnableDebugLog;
        public ConfigEntry<bool> EnableStackTraceInDebugLog;
        public Config(ConfigFile cfg)
        {
            cfg.SaveOnConfigSet = false;
            EnableDebugLog = cfg.Bind("Debug",
                                   "EnableDebugLog",
                                   defaultValue: false,
                                   "Enable the debug logs used for this mod.");
            EnableStackTraceInDebugLog = cfg.Bind("Debug",
                                   "EnableStackTraceInDebugLog",
                                   defaultValue: false,
                                   "Enable printing the stack trace in the error logs when using this mod.");

            ClearUnusedEntries(cfg);
            cfg.SaveOnConfigSet = true;
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