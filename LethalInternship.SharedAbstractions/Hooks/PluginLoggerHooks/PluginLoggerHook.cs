namespace LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks
{
    public delegate void PluginLogDebugDelegate(string message);
    public delegate void PluginLogInfoDelegate(string message);
    public delegate void PluginLogWarningDelegate(string message);
    public delegate void PluginLogErrorDelegate(string message);

    public class PluginLoggerHook
    {
        public static PluginLogDebugDelegate? LogDebug;
        public static PluginLogInfoDelegate? LogInfo;
        public static PluginLogWarningDelegate? LogWarning;
        public static PluginLogErrorDelegate? LogError;
    }
}
