using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(JesterAI))]
    public class JesterAIPatch
    {
        [HarmonyPatch("KillPlayerServerRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerServerRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld bool JesterAI::inKillAnimation")// 55
                    && codes[i + 1].ToString().StartsWith("brfalse"))//33
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsIdPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, codes[startIndex + 8].labels.First()) // br to 63
                };
                //-----------------------------
                codes.InsertRange(startIndex + 2, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.JesterAIPatch.KillPlayerServerRpc_Transpiler could not call check if intern.");
            }

            return codes.AsEnumerable();
        }
    }
}
