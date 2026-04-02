using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class SaveManagerProvider
    {
        private static ISaveManager? instance;

        public static bool IsReady => instance != null;

        public static ISaveManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("SaveManager not available yet");

                return instance;
            }
        }

        public static void Register(ISaveManager manager)
        {
            instance = manager;
        }

        public static void Unregister(ISaveManager manager)
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
