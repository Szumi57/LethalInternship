using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using ModelReplacement;
using System;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(BodyReplacementBase))]
    public class BodyReplacementBasePatch
    {
        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_Prefix(BodyReplacementBase __instance, ref GameObject ___replacementDeadBody)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.controller.playerClientId);
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
                    if (InternManagerProvider.Instance.HeldInternsLocalPlayer.Count > 1)
                    {
                        InternManagerProvider.Instance.HideShowRagdollModel(internAI.NpcController.Npc, show: false);
                    }
                }

                // Held intern with replacement body not null
                UpdateModelReplacement(__instance);
                return false;
            }

            Component instanceComponent = (Component)__instance; // Dodge the BodyReplacementBase compiler link with ListBodyReplacementOnDeadBodies
            if (__instance.controller.deadBody != null
                && !InternManagerProvider.Instance.ListBodyReplacementOnDeadBodies.Any(x => x.BodyReplacementBase == instanceComponent))
            {
                InternManagerProvider.Instance.ListBodyReplacementOnDeadBodies.Add(new BodyReplacementAdapter(instanceComponent));
                __instance.viewState.ReportBodyReplacementRemoval();
                __instance.cosmeticAvatar = __instance.ragdollAvatar;
                CreateAndParentRagdoll_ReversePatch(__instance, __instance.controller.deadBody);
                internAI.InternIdentity.BodyReplacementBase = __instance;
            }

            if (InternManagerProvider.Instance.ListBodyReplacementOnDeadBodies.Any(x => x.BodyReplacementBase == instanceComponent))
            {
                //PluginLoggerHook.LogDebug?.Invoke($"{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} only ragdoll update, {__instance.controller.deadBody}");
                UpdateModelReplacement(__instance);
                return false;
            }

            //PluginLoggerHook.LogDebug?.Invoke($"----------------{internAI.NpcController.Npc.playerUsername} {__instance.GetInstanceID()} all update");
            return true;
        }


        [HarmonyPatch("CreateAndParentRagdoll")]
        [HarmonyReversePatch]
        public static void CreateAndParentRagdoll_ReversePatch(object instance, DeadBodyInfo bodyinfo) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.ModelRplcmntAPI.BodyReplacementBasePatch.CreateAndParentRagdoll_ReversePatch");

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
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.controller.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            __result = internAI.NpcController.GetBoundsModel(model);
            return false;
        }
    }
}
