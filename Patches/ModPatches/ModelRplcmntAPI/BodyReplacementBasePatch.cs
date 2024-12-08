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
        //public static Dictionary<BodyReplacementBase, DeadBodyInfo> Dict = new Dictionary<BodyReplacementBase, DeadBodyInfo>();
        public static List<BodyReplacementBase> ListBodyReplacementOnDeadBodies = new List<BodyReplacementBase>();
        public static List<DeadBodyInfo> ListRagdollBodiesWithBodyReplacement = new List<DeadBodyInfo>();

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_Prefix(BodyReplacementBase __instance, ref GameObject ___replacementDeadBody)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (internAI.RagdollInternBody.IsRagdollBodyHeld())
            {
                // this dead body ??????
                // Held intern
                DeadBodyInfo? deadBodyInfo = internAI.RagdollInternBody.GetDeadBodyInfo();
                if (deadBodyInfo != null
                    && !ListRagdollBodiesWithBodyReplacement.Contains(deadBodyInfo))
                {
                    ListRagdollBodiesWithBodyReplacement.Add(deadBodyInfo);
                    __instance.cosmeticAvatar = __instance.ragdollAvatar;
                    CreateAndParentRagdoll_ReversePatch(__instance, deadBodyInfo);
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
                __instance.cosmeticAvatar = __instance.ragdollAvatar;
                CreateAndParentRagdoll_ReversePatch(__instance, __instance.controller.deadBody);
            }

            if (ListBodyReplacementOnDeadBodies.Contains(__instance))//___replacementDeadBody && __instance.controller.deadBody == null)
            {
                //__instance.cosmeticAvatar = __instance.avatar;
                //    Plugin.LogDebug("destroyed shit");
                //UnityEngine.Object.Destroy(___replacementDeadBody);
                //___replacementDeadBody = null!;

                __instance.avatar.Update();
                __instance.shadowAvatar.Update();
                __instance.ragdollAvatar.Update();
                __instance.viewModelAvatar.Update();
                return false;
            }

            return true;
        }


        [HarmonyPatch("CreateAndParentRagdoll")]
        [HarmonyReversePatch]
        public static void CreateAndParentRagdoll_ReversePatch(object instance, DeadBodyInfo bodyinfo) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch.CreateAndParentRagdoll_ReversePatch");


        //[HarmonyPatch("OnDestroy")]
        //[HarmonyPrefix]
        //static bool OnDestroy_Prefix(BodyReplacementBase __instance)
        //{
        //    UnityEngine.Object.Destroy(__instance.replacementModel);
        //    UnityEngine.Object.Destroy(__instance.replacementModelShadow);
        //    UnityEngine.Object.Destroy(__instance.replacementViewModel);

        //    Plugin.LogDebug($"destroy");
        //    return false;
        //}
    }
}
