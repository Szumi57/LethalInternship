using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class InputManagerProvider
    {
        private static IInputManager instance = null!;

        public static IInputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("Input manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
