using HarmonyLib;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace LethalInternship.Patches.ModPatches.ReviveCompany
{
    internal class ReviveCompanyPlayerControllerBPatchPatch
    {
        private static readonly MethodInfo IsGrabbableObjectEqualsToNullMethod = SymbolExtensions.GetMethodInfo(() => PatchesUtil.IsGrabbableObjectEqualsToNull((GrabbableObject)new object()));

        public static IEnumerable<CodeInstruction> SetHoverTipAndCurrentInteractTriggerPatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("call static RagdollGrabbableObject OPJosMod.ReviveCompany.GeneralUtil::GetClosestDeadBody") // 148
                    && codes[i + 1].ToString().StartsWith("stloc.s")
                    && codes[i + 2].ToString().StartsWith("ldsfld int OPJosMod.ReviveCompany.ConfigVariables::TimeUnitlCantBeRevived"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label label = generator.DefineLabel();
                codes[startIndex + 2].labels.Add(label);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, IsGrabbableObjectEqualsToNullMethod),
                    new CodeInstruction(OpCodes.Brfalse, label),
                    new CodeInstruction(OpCodes.Ret)
                };
                //-----------------------------
                codes.InsertRange(startIndex + 2, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ModPatches.ReviveCompany.ReviveCompanyPlayerControllerBPatchPatch.SetHoverTipAndCurrentInteractTriggerPatch_Transpiler could not check for closest dead body null");
            }

            for (int i = 0; i < codes.Count; i++)
            {
                Plugin.LogDebug($"{i} {codes[i]}");
            }

            return codes.AsEnumerable();
        }
    }
}
