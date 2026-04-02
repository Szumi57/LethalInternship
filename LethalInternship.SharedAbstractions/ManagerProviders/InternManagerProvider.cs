using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public static class InternManagerProvider
    {
        private static IInternManager? instance;

        public static bool IsReady => instance != null;

        public static IInternManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("InternManager not available yet");

                return instance;
            }
        }

        public static void Register(IInternManager manager)
        {
            instance = manager;
        }

        public static void Unregister(IInternManager manager)
        {
            if (instance == manager)
                instance = null;
        }

        public static void ForceClear()
        {
            instance = null;
        }
    }
}
