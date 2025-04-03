using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public class GrabbableObjectPatch
    {
        [HarmonyPatch("SetControlTipsForItem")]
        [HarmonyPrefix]
        static bool SetControlTipsForItem_PreFix(GrabbableObject __instance)
        {
            return InternManager.Instance.IsAnInternAiOwnerOfObject(__instance);
        }

        [HarmonyPatch("DiscardItem")]
        [HarmonyPrefix]
        static bool DiscardItem_PreFix(GrabbableObject __instance)
        {
            PlayerControllerB? internController = __instance.playerHeldBy;
            if (internController == null
                || !InternManager.Instance.IsPlayerIntern(internController))
            {
                return true;
            }

            __instance.playerHeldBy.IsInspectingItem = false;
            __instance.playerHeldBy.activatingItem = false;
            __instance.playerHeldBy = null;
            return false;
        }

        /// <summary>
        /// ScanNodeProperties can be null, so perfix patch to cover cases with ragdoll bodies only
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("SetScrapValue")]
        [HarmonyPrefix]
        static bool SetScrapValue_PreFix(GrabbableObject __instance, int setValueTo)
        {
            RagdollGrabbableObject? ragdollGrabbableObject = __instance as RagdollGrabbableObject;
            if (ragdollGrabbableObject == null)
            {
                // Other scrap = do base game logic
                return true;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI(ragdollGrabbableObject.bodyID.Value);
            if (internAI == null)
            {
                if (ragdollGrabbableObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null)
                {
                    // ragdoll of irl player with ScanNodeProperties null, we do the base game logic without the error
                    __instance.scrapValue = setValueTo;
                    return false;
                }

                return true;
            }

            if (internAI.NpcController.Npc.isPlayerDead
                && ragdollGrabbableObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null)
            {
                if (ragdollGrabbableObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null)
                {
                    // ragdoll of intern with ScanNodeProperties null, we do the base game logic without the error
                    __instance.scrapValue = setValueTo;
                    return false;
                }

                return true;
            }

            // Grabbable ragdoll body, not sellable, intern not dead
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(nameof(GrabbableObject.Update))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GrabbableObject_Update_ReversePatch(RagdollGrabbableObject instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.GrabbableObjectPatch.GrabbableObject_Update_ReversePatch");

        [HarmonyPatch("EquipItem")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> EquipItem_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()"//3
                    && codes[i + 1].ToString() == "callvirt void HUDManager::ClearControlTips()"
                    && codes[i + 3].ToString() == "callvirt virtual void GrabbableObject::SetControlTipsForItem()")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnInternAiOwnerOfObjectMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + 4].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ObjectsPatches.EquipItem_Transpiler could not remove check if holding player is intern");
            }

            return codes.AsEnumerable();
        }
    }
}
