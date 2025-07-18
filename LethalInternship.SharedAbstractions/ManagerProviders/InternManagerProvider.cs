using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public static class InternManagerProvider
    {
        private static IInternManager instance = null!;

        public static IInternManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("Intern manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
