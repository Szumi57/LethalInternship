using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using System;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    internal class ManualCameraRendererPatch
    {
        [HarmonyPatch("GetRadarTargetIndexPlusOne")]
        [HarmonyReversePatch]
        public static int GetRadarTargetIndexPlusOne_ReversePatch(object instance, int index) => throw new NotImplementedException("Stub LethalInternship.Patches.MapPatches.ManualCameraRendererPatch.GetRadarTargetIndexPlusOne_ReversePatch");

        [HarmonyPatch("GetRadarTargetIndexMinusOne")]
        [HarmonyReversePatch]
        public static int GetRadarTargetIndexMinusOne_ReversePatch(object instance, int index) => throw new NotImplementedException("Stub LethalInternship.Patches.MapPatches.ManualCameraRendererPatch.SetPlayerTeleporterId_ReversePatch");

        [HarmonyPatch("GetRadarTargetIndexPlusOne")]
        [HarmonyPostfix]
        static void GetRadarTargetIndexPlusOne_PostFix(ManualCameraRenderer __instance,
                                                       ref int __result)
        {
            InternAI? internAI;
            for (int i = 0; i < __instance.radarTargets.Count; i++)
            {
                // radar target can have radar booster in it
                PlayerControllerB controller = __instance.radarTargets[__result].transform.gameObject.GetComponent<PlayerControllerB>();
                if (controller == null)
                {
                    continue;
                }

                internAI = InternManager.Instance.GetInternAI((int)controller.playerClientId);
                if (internAI != null)
                {
                    if (!Plugin.Config.RadarEnabled.Value
                        || internAI.RagdollInternBody.IsRagdollBodyHeld())
                    {
                        __result = GetRadarTargetIndexPlusOne_ReversePatch(__instance, __result);
                    }
                    else
                    {
                        // valid intern
                        break;
                    }
                }
                else
                {
                    // player
                    break;
                }
            }
        }

        [HarmonyPatch("GetRadarTargetIndexMinusOne")]
        [HarmonyPostfix]
        static void GetRadarTargetIndexMinusOne_PostFix(ManualCameraRenderer __instance,
                                                       ref int __result)
        {
            InternAI? internAI;
            for (int i = 0; i < __instance.radarTargets.Count; i++)
            {
                internAI = InternManager.Instance.GetInternAI((int)__instance.radarTargets[__result].transform.gameObject.GetComponent<PlayerControllerB>().playerClientId);
                if (internAI != null)
                {
                    if (!Plugin.Config.RadarEnabled.Value
                        || internAI.RagdollInternBody.IsRagdollBodyHeld())
                    {
                        __result = GetRadarTargetIndexMinusOne_ReversePatch(__instance, __result);
                    }
                    else
                    {
                        // valid intern
                        break;
                    }
                }
                else
                {
                    // player
                    break;
                }
            }
        }
    }
}
