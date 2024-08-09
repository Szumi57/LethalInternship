using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using MoreEmotes.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches
{
    [HarmonyPatch(typeof(EmotePatch))]
    internal class MoreEmotesPatch
    {
        [HarmonyPatch("UpdatePrefix")]
        [HarmonyPrefix]
        static void UpdatePrefix_Prefix(ref bool[] ___s_wasPerformingEmote)
        {
            int allEntitiesCount = InternManager.Instance.AllEntitiesCount;
            if (___s_wasPerformingEmote!= null 
                && ___s_wasPerformingEmote.Length < allEntitiesCount)
            {
                Array.Resize(ref ___s_wasPerformingEmote, allEntitiesCount);
            }
        }

        [HarmonyPatch("UpdatePostfix")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UpdatePostfix_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = 0;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            Label labelToJumpTo = generator.DefineLabel();
            codes[startIndex].labels.Add(labelToJumpTo);

            List<CodeInstruction> codesToAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                new CodeInstruction(OpCodes.Brfalse, labelToJumpTo),
                new CodeInstruction(OpCodes.Ret, null)
            };
            codes.InsertRange(startIndex, codesToAdd);

            return codes.AsEnumerable();
        }
    }
}
