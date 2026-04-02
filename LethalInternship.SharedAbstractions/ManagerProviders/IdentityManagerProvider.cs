using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public static class IdentityManagerProvider
    {
        private static IIdentityManager? instance;

        public static bool IsReady => instance != null;

        public static IIdentityManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("IdentityManager not available yet");

                return instance;
            }
        }

        public static void Register(IIdentityManager manager)
        {
            instance = manager;
        }

        public static void Unregister(IIdentityManager manager)
        {
            if (instance == manager)
                instance = null;
        }
    }
}
