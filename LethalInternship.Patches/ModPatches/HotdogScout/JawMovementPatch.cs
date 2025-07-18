using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.HotdogScout
{
    [HarmonyPatch(typeof(JawMovement))]
    public class JawMovementPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_Prefix(JawMovement __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            float num = 0f;
            if (internAI.InternIdentity.Voice.IsTalking())
            {
                num = __instance.player.isPlayerDead ? 0f : Mathf.Clamp(internAI.InternIdentity.Voice.GetAmplitude() * __instance.sensibility, 0f, __instance.maxJawOpening);
            }
            __instance.skinnedMeshRenderer.SetBlendShapeWeight(__instance.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(__instance.blendShapeName), num);

            return false;
        }
    }
}
