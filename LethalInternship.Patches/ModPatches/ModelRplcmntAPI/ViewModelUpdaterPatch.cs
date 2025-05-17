using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using ModelReplacement.Scripts.Player;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ViewModelUpdater))]
    public class ViewModelUpdaterPatch
    {
        [HarmonyPatch("AssignViewModelReplacement")]
        [HarmonyPrefix]
        static bool AssignViewModelReplacement_Prefix(GameObject player, ref GameObject replacementViewModel)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)player.GetComponent<PlayerControllerB>().playerClientId);
            if (internAI == null)
            {
                return true;
            }

            replacementViewModel = null!;
            return true;
        }
    }
}
