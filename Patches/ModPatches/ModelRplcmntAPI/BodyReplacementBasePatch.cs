using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(BodyReplacementBase))]
    internal class BodyReplacementBasePatch
    {
        public static List<BodyReplacementBase> ListBodyReplacementOnDeadBodies = new List<BodyReplacementBase>();

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_Prefix(BodyReplacementBase __instance, ref GameObject ___replacementDeadBody)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (internAI.RagdollInternBody.IsRagdollEnabled())
            {
                // Held intern
                DeadBodyInfo? heldDeadBodyInfo = internAI.RagdollInternBody.GetDeadBodyInfo();
                if (heldDeadBodyInfo != null
                    && ___replacementDeadBody == null)
                {
                    __instance.cosmeticAvatar = __instance.ragdollAvatar;
                    CreateAndParentRagdoll_ReversePatch(__instance, heldDeadBodyInfo);
                }

                // Held intern with replacement body not null
                __instance.avatar.Update();
                __instance.shadowAvatar.Update();
                __instance.ragdollAvatar.Update();
                __instance.viewModelAvatar.Update();
                return false;
            }

            if (__instance.controller.deadBody != null
                && !ListBodyReplacementOnDeadBodies.Contains(__instance))
            {
                //Dict[__instance] = __instance.controller.deadBody;
                ListBodyReplacementOnDeadBodies.Add(__instance);
                __instance.viewState.ReportBodyReplacementRemoval();
                __instance.cosmeticAvatar = __instance.ragdollAvatar;
                CreateAndParentRagdoll_ReversePatch(__instance, __instance.controller.deadBody);
            }


            if (ListBodyReplacementOnDeadBodies.Contains(__instance))//___replacementDeadBody && __instance.controller.deadBody == null)
            {
                //Plugin.LogDebug($"{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} only ragdoll update");
                __instance.ragdollAvatar.Update();
                return false;
            }

            //Plugin.LogDebug($"{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} all update");
            return true;
        }


        [HarmonyPatch("CreateAndParentRagdoll")]
        [HarmonyReversePatch]
        public static void CreateAndParentRagdoll_ReversePatch(object instance, DeadBodyInfo bodyinfo) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch.CreateAndParentRagdoll_ReversePatch");
    }
}
