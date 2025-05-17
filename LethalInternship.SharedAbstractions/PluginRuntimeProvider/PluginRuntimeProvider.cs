using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.SharedAbstractions.PluginRuntimeProvider
{
    public static class PluginRuntimeProvider
    {
        private static IPluginRuntimeContext context = null!;
        public static IPluginRuntimeContext Context
        {
            get
            {
                if (context == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("PluginRuntimeContext not initialized !");
                    return null!;
                }
                return context;
            }

            set => context = value;
        }
    }
}
