using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class TerminalManagerProvider
    {
        private static ITerminalManager? instance;

        public static bool IsReady => instance != null;

        public static ITerminalManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("TerminalManager not available yet");

                return instance;
            }
        }

        public static void Register(ITerminalManager manager)
        {
            instance = manager;
        }

        public static void Unregister(ITerminalManager manager)
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
