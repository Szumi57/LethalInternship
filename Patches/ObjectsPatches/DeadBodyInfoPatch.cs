using HarmonyLib;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(DeadBodyInfo))]
    internal class DeadBodyInfoPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Start_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //65
                    && codes[i + 3].ToString().StartsWith("ldarg.0 NULL")//68
                    && codes[i + 8].ToString() == "ldstr \"PlayerRagdoll\"")//73
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<Label> labelsOfCodeToJumpTo = codes[startIndex + 3].labels;

                // Define label for the jump
                Label labelToJumpTo = generator.DefineLabel();
                labelsOfCodeToJumpTo.Add(labelToJumpTo);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                                                        {
                                                            new CodeInstruction(OpCodes.Ldarg_0, null),
                                                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DeadBodyInfo), "playerObjectId")),
                                                            new CodeInstruction(OpCodes.Call, PatchesUtil.IsIdPlayerInternMethod),
                                                            new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
                                                        };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.ObjectsPatches.DeadBodyInfoPatch.Start_Transpiler remplace with correct tag if intern.");
            }

            // ----------------------------------------------------------------------
            return codes.AsEnumerable();
        }
    }
}
