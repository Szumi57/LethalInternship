using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class SaveManagerProvider
    {
        private static ISaveManager instance = null!;

        public static ISaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("Save manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
