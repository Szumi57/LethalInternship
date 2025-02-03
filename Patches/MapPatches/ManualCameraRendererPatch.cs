using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using System;
using System.Linq;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    public class ManualCameraRendererPatch
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
            TransformAndName radarTarget = __instance.radarTargets[__result];
            if (radarTarget == null)
            {
                return;
            }

            PlayerControllerB controller = radarTarget.transform.gameObject.GetComponent<PlayerControllerB>();
            if (controller == null)
            {
                // radar target can have radar booster in it
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)controller.playerClientId);
            if (internAI == null)
            {
                if ((int)controller.playerClientId >= InternManager.Instance.IndexBeginOfInterns)
                {
                    // actually intern but invalid
                    __result = GetRadarTargetIndexPlusOne_ReversePatch(__instance, __result);
                    return;
                }

                // player
                return;
            }

            // Intern
            if (!Plugin.Config.RadarEnabled)
            {
                __result = GetRadarTargetIndexPlusOne_ReversePatch(__instance, __result);
                return;
            }

            int[] idsIdentitiesSpawned = IdentityManager.Instance.GetIdentitiesSpawned();
            if (idsIdentitiesSpawned.Contains(internAI.InternIdentity.IdIdentity))
            {
                // valid intern
                return;
            }

            // intern not valid
            __result = 0;
            return;
        }

        [HarmonyPatch("GetRadarTargetIndexMinusOne")]
        [HarmonyPostfix]
        static void GetRadarTargetIndexMinusOne_PostFix(ManualCameraRenderer __instance,
                                                       ref int __result)
        {
            TransformAndName radarTarget = __instance.radarTargets[__result];
            if (radarTarget == null)
            {
                return;
            }

            PlayerControllerB controller = radarTarget.transform.gameObject.GetComponent<PlayerControllerB>();
            if (controller == null)
            {
                // radar target can have radar booster in it
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)controller.playerClientId);
            if (internAI == null)
            {
                if ((int)controller.playerClientId >= InternManager.Instance.IndexBeginOfInterns)
                {
                    // actually intern but invalid
                    __result = GetRadarTargetIndexMinusOne_ReversePatch(__instance, __result);
                    return;
                }

                // player
                return;
            }

            // Intern
            if (!Plugin.Config.RadarEnabled)
            {
                __result = GetRadarTargetIndexMinusOne_ReversePatch(__instance, __result);
                return;
            }

            int[] idsIdentitiesSpawned = IdentityManager.Instance.GetIdentitiesSpawned();
            if (idsIdentitiesSpawned.Contains(internAI.InternIdentity.IdIdentity))
            {
                // valid intern
                return;
            }

            // intern not valid
            __result = 0;
            return;
        }
    }
}
