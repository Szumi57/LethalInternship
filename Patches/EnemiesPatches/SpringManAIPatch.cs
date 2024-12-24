using HarmonyLib;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>SpringManAI</c>
    /// </summary>
    [HarmonyPatch(typeof(SpringManAI))]
    internal class SpringManAIPatch
    {
        /// <summary>
        /// Make the sping man use all array of player + interns to target
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].ToString() == "ldc.i4.4 NULL" || codes[i].ToString() == "ldsfld int MoreCompany.MainClass::newPlayerCount")//110
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Call;
                codes[startIndex].operand = PatchesUtil.AllEntitiesCountMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.SpringManAIPatch.Update_Transpiler could not change size of player array to look up.");
            }

            return codes.AsEnumerable();
        }
    }
}
