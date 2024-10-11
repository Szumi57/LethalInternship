using HarmonyLib;
using LethalInternship.Utils;
using ModelReplacement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ModelReplacementAPI))]
    internal class ModelReplacementAPIPatch
    {
        [HarmonyPatch("SetPlayerModelReplacement")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SetPlayerModelReplacement_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 23; i++)
            {
                if (codes[i].ToString().StartsWith("call static bool ModelReplacement.ModelReplacementAPI::get_IsLan()") // 21
                    && codes[i + 23].ToString().StartsWith("ldarg.0 NULL")) // 44
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 23].labels.Add(labelToJumpTo);

                // Adds dummy line for label that land here
                codes.Insert(startIndex + 1, new CodeInstruction(codes[startIndex].opcode, codes[startIndex].operand));
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                startIndex++;

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIPatch.SetPlayerModelReplacement_Transpiler could not bypass is lan and player steam id 0 when intern.");
            }

            return codes.AsEnumerable();
        }
    }
}
