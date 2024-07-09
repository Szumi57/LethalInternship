using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>SandSpiderAI</c>
    /// </summary>
    [HarmonyPatch(typeof(SandSpiderAI))]
    internal class SandSpiderAIPatch
    {
        /// <summary>
        /// Patch to make the spider use the right player id for hurting the player(intern) instead of only localPlayerController id.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnCollideWithPlayer_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL"//40
                    && codes[i + 1].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 5].ToString() == "call void SandSpiderAI::HitPlayerServerRpc(int playerId)")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Ldloc_0;
                codes[startIndex + 2].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.SandSpiderAIPatch.OnCollideWithPlayer_Transpiler could not change use of correct player id for HitPlayerServerRpc.");
            }

            return codes.AsEnumerable();
        }
    }
}
