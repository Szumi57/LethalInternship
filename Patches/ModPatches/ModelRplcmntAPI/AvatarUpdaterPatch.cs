using HarmonyLib;
using LethalInternship.Interns;
using LethalInternship.Managers;
using ModelReplacement.AvatarBodyUpdater;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(AvatarUpdater))]
    public class AvatarUpdaterPatch
    {
        [HarmonyPatch("UpdateModel")]
        [HarmonyPrefix]
        static bool UpdateModel_Prefix(AvatarUpdater __instance,
                                       GameObject ___player,
                                       SkinnedMeshRenderer ___playerModelRenderer,
                                       Vector3 ___rootPositionOffset)
        {
            // Cull animations ?
            InternCullingBodyInfo? internCullingBodyInfo = InternManager.Instance.GetInternCullingBodyInfo(___player.gameObject);
            if (internCullingBodyInfo == null)
            {
                return true;
            }

            internCullingBodyInfo.UpdateAnimationCullingModelReplacement(__instance,
                                                                         ___rootPositionOffset,
                                                                         ___playerModelRenderer);
            return false;
        }
    }
}
