using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class UIManagerProvider
    {
        private static IUIManager? instance;

        public static bool IsReady => instance != null;

        public static IUIManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("UIManager not available yet");

                return instance;
            }
        }

        public static void Register(IUIManager manager)
        {
            instance = manager;
        }

        public static void Unregister(IUIManager manager)
        {
            if (instance == manager)
                instance = null;
        }
    }
}
