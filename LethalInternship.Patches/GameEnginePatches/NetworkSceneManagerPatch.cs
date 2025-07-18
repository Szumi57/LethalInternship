using HarmonyLib;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using Unity.Netcode;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>NetworkSceneManager</c>
    /// </summary>
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    public class NetworkSceneManagerPatch
    {
        /// <summary>
        /// Patch for populate the pool of interns at the start of the load scene
        /// </summary>
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix()
        {
            if (PluginRuntimeProvider.Context.IsModMoreCompanyLoaded)
            {
                UpdateIrlPlayerAfterMoreCompany();
            }

            InternManagerProvider.Instance.ManagePoolOfInterns();
        }

        private static void UpdateIrlPlayerAfterMoreCompany()
        {
            PluginRuntimeProvider.Context.PluginIrlPlayersCount = MoreCompany.MainClass.newPlayerCount;
            PluginLoggerHook.LogDebug?.Invoke($"PluginIrlPlayersCount after morecompany = {PluginRuntimeProvider.Context.PluginIrlPlayersCount}");
        }
    }
}
