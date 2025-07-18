using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.SharedAbstractions.ManagerProviders
{
    public class UIManagerProvider
    {
        private static IUIManager instance = null!;

        public static IUIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Error
                    PluginLoggerHook.LogError?.Invoke("UI manager not initialized !");
                    return null!;
                }
                return instance;
            }

            set => instance = value;
        }
    }
}
