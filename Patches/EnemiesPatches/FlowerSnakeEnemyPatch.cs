using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for <c>FlowerSnakeEnemy</c>
    /// </summary>
    [HarmonyPatch(typeof(FlowerSnakeEnemy))]
    internal class FlowerSnakeEnemyPatch
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
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //62
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 4].ToString() == "call void FlowerSnakeEnemy::FSHitPlayerServerRpc(int playerId)") //66
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
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.FlowerSnakeEnemyPatch.OnCollideWithPlayer_Transpiler could not use id player local or intern");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch to make the snake to be able to make intern fly
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("MainSnakeActAsConductor")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MainSnakeActAsConductor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 32; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //8
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 32].ToString() == "call void FlowerSnakeEnemy::StopClingingServerRpc(int playerId)") //40
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
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.FlowerSnakeEnemyPatch.MainSnakeActAsConductor_Transpiler could not use id player local or intern");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch for manipulating field to make snake able to make intern fly
        /// </summary>
        /// <param name="___waitingForHitPlayerRPC"></param>
        /// <param name="playerId"></param>
        [HarmonyPatch("FSHitPlayerServerRpc")]
        [HarmonyPostfix]
        static void FSHitPlayerServerRpc_PostFix(ref bool ___waitingForHitPlayerRPC, int playerId)
        { 
            if(InternManager.Instance.IsIdPlayerInternOwnerLocal(playerId)
                && ___waitingForHitPlayerRPC)
            {
                ___waitingForHitPlayerRPC = false;
            }
        }

        /// <summary>
        /// Patch for manipulating field to make snake able to make intern fly
        /// </summary>
        /// <param name="___waitingForHitPlayerRPC"></param>
        /// <param name="playerId"></param>
        [HarmonyPatch("ClingToPlayerClientRpc")]
        [HarmonyPostfix]
        static void ClingToPlayerClientRpc_PostFix(ref bool ___waitingForHitPlayerRPC, int playerId)
        {
            if (InternManager.Instance.IsIdPlayerInternOwnerLocal(playerId)
                && ___waitingForHitPlayerRPC)
            {
                ___waitingForHitPlayerRPC = false;
            }
        }
    }
}
