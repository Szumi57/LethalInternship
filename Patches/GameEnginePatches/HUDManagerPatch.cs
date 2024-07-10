using HarmonyLib;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for the <c>HUDManager</c>
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        /// <summary>
        /// Patch for making the hud only show end games stats for irl players, not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("FillEndGameStats")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FillEndGameStats_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //170
                    && codes[i + 1].ToString() == "ldfld StartOfRound HUDManager::playersManager"
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 3].ToString() == "ldlen NULL")
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
                Plugin.LogError($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.FillEndGameStats_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }
    }
}
