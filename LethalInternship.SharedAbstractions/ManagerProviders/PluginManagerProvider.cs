using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class PluginManagerProvider
    {
        private static IPluginManager? instance;

        public static bool IsReady => instance != null;

        public static IPluginManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("PluginManager not available yet");

                return instance;
            }
        }

        public static void Register(IPluginManager manager)
        {
            instance = manager;
        }

        public static void Unregister(IPluginManager manager)
        {
            if (instance == manager)
                instance = null;
        }
    }
}
