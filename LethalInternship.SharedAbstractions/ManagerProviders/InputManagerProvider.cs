using LethalInternship.SharedAbstractions.Managers;
using System;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class InputManagerProvider
    {
        private static IInputManager? instance;

        public static bool IsReady => instance != null;

        public static IInputManager Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("InputManager not available yet");

                return instance;
            }
        }

        public static void Register(IInputManager manager)
        {
            instance = manager;
        }

        public static void Unregister(IInputManager manager)
        {
            if (instance == manager)
                instance = null;
        }
    }
}
