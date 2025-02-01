using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Managers;
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
            if (Plugin.IsModMoreCompanyLoaded)
            {
                UpdateIrlPlayerAfterMoreCompany();
            }

            InternManager.Instance.ManagePoolOfInterns();
        }

        private static void UpdateIrlPlayerAfterMoreCompany()
        {
            Plugin.PluginIrlPlayersCount = MoreCompany.MainClass.newPlayerCount;
            Plugin.LogDebug($"PluginIrlPlayersCount after morecompany = {Plugin.PluginIrlPlayersCount}");
        }
    }
}
