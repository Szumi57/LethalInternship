using HarmonyLib;
using LethalInternship.Managers;
using System.Collections.Generic;
using Unity.Netcode;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(MoreCompany.PluginInformation.PLUGIN_GUID)]
    internal class NetworkSceneManagerPatch
    {
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix(ref Dictionary<uint, Dictionary<int, NetworkObject>> ___ScenePlacedObjects)
        {
            InternManager.Instance.ResizeAndPopulateInterns();
        }
    }
}
