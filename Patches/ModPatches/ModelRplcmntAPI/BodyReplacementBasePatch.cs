using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement;
using System;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(BodyReplacementBase))]
    internal class BodyReplacementBasePatch
    {
        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_Prefix(BodyReplacementBase __instance, ref GameObject ___replacementDeadBody)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (!internAI.RagdollInternBody.IsRagdollBodyHeld())
            {
                if (___replacementDeadBody && __instance.controller.deadBody == null)
                {
                    __instance.cosmeticAvatar = __instance.avatar;
                    UnityEngine.Object.Destroy(___replacementDeadBody);
                    ___replacementDeadBody = null!;
                }

                return true;
            }

            // Held intern
            if (___replacementDeadBody == null)
            {
                DeadBodyInfo? deadBodyInfo = internAI.RagdollInternBody.GetDeadBodyInfo();
                if (deadBodyInfo != null)
                {
                    __instance.cosmeticAvatar = __instance.ragdollAvatar;
                    CreateAndParentRagdoll_ReversePatch(__instance, deadBodyInfo);
                }
            }

            // Held intern with replacement body not null
            __instance.avatar.Update();
            __instance.shadowAvatar.Update();
            __instance.ragdollAvatar.Update();
            __instance.viewModelAvatar.Update();
            return false;
        }


        [HarmonyPatch("CreateAndParentRagdoll")]
        [HarmonyReversePatch]
        public static void CreateAndParentRagdoll_ReversePatch(object instance, DeadBodyInfo bodyinfo) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch.CreateAndParentRagdoll_ReversePatch");

    }
}
