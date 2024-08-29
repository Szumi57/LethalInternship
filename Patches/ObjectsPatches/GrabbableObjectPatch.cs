using HarmonyLib;
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
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch("SetControlTipsForItem")]
        [HarmonyPrefix]
        static bool SetControlTipsForItem_PreFix(GrabbableObject __instance)
        {
            return InternManager.Instance.IsAnInternAiOwnerOfObject(__instance);
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
