using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>RedLocustBees</c>
    /// </summary>
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class RedLocustBeesPatch
    {
        /// <summary>
        /// <inheritdoc cref="ButlerBeesEnemyAIPatch.OnCollideWithPlayer_Transpiler"/>
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
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//29
                    && codes[i + 4].ToString() == "call void RedLocustBees::BeeKillPlayerOnLocalClient(int playerId)")//33
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldloc_0;
                codes[startIndex + 1].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.RedLocustBeesPatch.OnCollideWithPlayer_Transpiler could not call BeeKillPlayerOnLocalClient on player id.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//35
                    && codes[i + 4].ToString() == "call void RedLocustBees::BeeKillPlayerServerRpc(int playerId)")//39
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldloc_0;
                codes[startIndex + 1].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.RedLocustBeesPatch.OnCollideWithPlayer_Transpiler could not call BeeKillPlayerServerRpc on player id.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//60
                    && codes[i + 4].ToString() == "call void RedLocustBees::EnterAttackZapModeServerRpc(int clientWhoSent)")//64
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldloc_0;
                codes[startIndex + 1].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.RedLocustBeesPatch.OnCollideWithPlayer_Transpiler could not call EnterAttackZapModeServerRpc player on player id.");
            }

            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"OnCollideWithPlayer ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"OnCollideWithPlayer ======================");

            return codes.AsEnumerable();
        }
    }
}
