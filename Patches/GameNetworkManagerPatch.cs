using HarmonyLib;
using LethalInternship.Managers;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch 
    {
        [HarmonyPatch("SaveGame")]
        [HarmonyPostfix]
        public static void SaveGame_Postfix()
        {
            SaveManager.Instance.SavePluginInfos();
        }
    }
}
