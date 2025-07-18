using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class TerminalManagerProvider
    {
        private static ITerminalManager instance = null!;

        public static ITerminalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("Terminal manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
