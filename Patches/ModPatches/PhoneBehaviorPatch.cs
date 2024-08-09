using HarmonyLib;
using LethalInternship.Utils;
using Scoops.misc;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches
{
    [HarmonyPatch(typeof(PhoneBehavior))]
    internal class PhoneBehaviorPatch
    {
        [HarmonyPatch("UpdatePlayerVoices")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UpdatePlayerVoices_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()" // 336
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Nop;
                codes[startIndex + 2].operand = null;
                codes[startIndex + 3].opcode = OpCodes.Call;
                codes[startIndex + 3].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ModPatches.PhoneBehaviorPatch.UpdatePlayerVoices_Transpiler could not check only for irl players not interns.");
            }

            return codes.AsEnumerable();
        }
    }
}
