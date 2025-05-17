using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using ModelReplacement;
using UnityEngine.Rendering;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ViewStateManager))]
    public class ViewStateManagerPatch
    {
        [HarmonyPatch("UpdateModelReplacement")]
        [HarmonyPrefix]
        static bool UpdateModelReplacement_Prefix(ViewStateManager __instance,
                                                  PlayerControllerB ___controller,
                                                  int ___CullingMaskThirdPerson)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)___controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            // Disable base player model
            if (___controller.thisPlayerModel.enabled)
            {
                __instance.SetPlayerRenderers(enabled: false, helmetShadow: false);
                __instance.SetPlayerLayers(ViewStateManager.modelLayer);
                __instance.SetShadowModel(false);
                __instance.SetArmLayers(__instance.InvisibleLayer);
                __instance.SetAvatarLayers(__instance.VisibleLayer, ShadowCastingMode.On);
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    ___controller.gameplayCamera.cullingMask = ___CullingMaskThirdPerson;
                }
            }

            return false;
        }

        [HarmonyPatch("UpdatePlayer")]
        [HarmonyPrefix]
        static bool UpdatePlayer_Prefix(ViewStateManager __instance,
                                        PlayerControllerB ___controller,
                                        int ___CullingMaskFirstPerson)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)___controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            // Enable base player model
            if (!___controller.thisPlayerModel.enabled)
            {
                __instance.SetPlayerRenderers(enabled: true, helmetShadow: true);
                ___controller.gameplayCamera.cullingMask = ___CullingMaskFirstPerson;
                __instance.SetPlayerLayers(ViewStateManager.visibleLayer);
                __instance.SetArmLayers(__instance.InvisibleLayer);
            }

            return false;
        }
    }
}
