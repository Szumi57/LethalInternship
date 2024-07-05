using HarmonyLib;
using LethalInternship.Managers;
using Unity.Netcode;

namespace LethalInternship.Patches.GameEnginePatches
{
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class NetworkSceneManagerPatch
    {
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix()
        {
            InternManager.Instance.ManagePoolOfInterns();
        }
    }
}
