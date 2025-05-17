using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class IdentityManagerProvider
    {
        private static IIdentityManager instance = null!;

        public static IIdentityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("Identity manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
