using HarmonyLib;
using LethalInternship.Managers;
using Unity.Netcode;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>NetworkSceneManager</c>
    /// </summary>
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class NetworkSceneManagerPatch
    {
        /// <summary>
        /// Patch for populate the pool of interns at the start of the load scene
        /// </summary>
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix()
        {
            InternManager.Instance.ManagePoolOfInterns();
        }
    }
}
