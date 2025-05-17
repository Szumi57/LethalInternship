using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for the <c>ButlerBeesEnemyAI</c>
    /// </summary>
    [HarmonyPatch(typeof(ButlerBeesEnemyAI))]
    public class ButlerBeesEnemyAIPatch
    {
        /// <summary>
        /// Patch onCollide to check for player and intern
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnCollideWithPlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "ldloc.0 NULL" //30
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") //32
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;
                codes[startIndex + 3].opcode = OpCodes.Nop;
                codes[startIndex + 3].operand = null;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.ButlerBeesEnemyAIPatch.OnCollideWithPlayer_Transpiler could not check if local player or intern owner local player");
            }

            return codes.AsEnumerable();
        }
    }
}
