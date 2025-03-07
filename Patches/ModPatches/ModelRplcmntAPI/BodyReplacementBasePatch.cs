using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(BodyReplacementBase))]
    public class BodyReplacementBasePatch
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
                UpdateModelReplacement(__instance);
                return false;
            }

            if (__instance.controller.deadBody != null
                && !ListBodyReplacementOnDeadBodies.Contains(__instance))
            {
                ListBodyReplacementOnDeadBodies.Add(__instance);
                __instance.viewState.ReportBodyReplacementRemoval();
                __instance.cosmeticAvatar = __instance.ragdollAvatar;
                CreateAndParentRagdoll_ReversePatch(__instance, __instance.controller.deadBody);
                internAI.InternIdentity.BodyReplacementBase = __instance;
            }

            if (ListBodyReplacementOnDeadBodies.Contains(__instance))
            {
                //Plugin.LogDebug($"{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} only ragdoll update, {__instance.controller.deadBody}");
                UpdateModelReplacement(__instance);
                return false;
            }

            //Plugin.LogDebug($"----------------{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} all update");
            return true;
        }


        [HarmonyPatch("CreateAndParentRagdoll")]
        [HarmonyReversePatch]
        public static void CreateAndParentRagdoll_ReversePatch(object instance, DeadBodyInfo bodyinfo) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch.CreateAndParentRagdoll_ReversePatch");

        public static void CleanListBodyReplacementOnDeadBodies()
        {
            for (int i = 0; i < ListBodyReplacementOnDeadBodies.Count; i++)
            {
                var bodyReplacementBase = ListBodyReplacementOnDeadBodies[i];
                if (bodyReplacementBase == null
                    || bodyReplacementBase.deadBody == null)
                {
                    continue;
                }

                if (!StartOfRound.Instance.shipBounds.bounds.Contains(bodyReplacementBase.deadBody.transform.position))
                {
                    bodyReplacementBase.IsActive = false;
                    UnityEngine.Object.Destroy(bodyReplacementBase);
                    ListBodyReplacementOnDeadBodies[i] = null!;
                }
            }
            ListBodyReplacementOnDeadBodies = ListBodyReplacementOnDeadBodies.Where(x => x != null 
                                                                                      && x.deadBody != null).ToList();
        }

        private static void UpdateModelReplacement(BodyReplacementBase bodyReplacement)
        {
            bodyReplacement.ragdollAvatar.Update();
            bodyReplacement.avatar.Update();
            //bodyReplacement.shadowAvatar.Update(); // no shadow for interns
            //bodyReplacement.viewModelAvatar.Update(); // No view model (1st person view) for interns
        }

        [HarmonyPatch("GetBounds")]
        [HarmonyPrefix]
        static bool GetBounds_Prefix(BodyReplacementBase __instance, GameObject model, ref Bounds __result)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            __result = internAI.NpcController.GetBoundsTimedCheck.GetBoundsModel(model);
            return false;
        }
    }
}
