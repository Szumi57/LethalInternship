using AdvancedFeatures;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches.AdvancedFeatures
{
    [HarmonyPatch(typeof(Endscreen))]
    public class EndscreenPatch
    {
        [HarmonyPatch("Open")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Open_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.s 17") // 165
                    && codes[i + 1].ToString().StartsWith("ldfld bool GameNetcodeStuff.PlayerControllerB::disconnectedMidGame"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(codes[startIndex]), // ldloc.s 17 (PlayerControllerB)
                    new CodeInstruction(OpCodes.Call, PatchesUtil.ShouldIgnoreInternsEndScreenMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[484].labels[0]) // continue (for loop)
                };

                //-----------------------------
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.AdvancedFeatures.EndscreenPatch.Open_Transpiler could not ignore end screen recap when intern");
            }

            return codes.AsEnumerable();
        }
    }
}
