using HarmonyLib;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for <c>FlowerSnakeEnemy</c>
    /// </summary>
    [HarmonyPatch(typeof(FlowerSnakeEnemy))]
    public class FlowerSnakeEnemyPatch
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
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.FlowerSnakeEnemyPatch.OnCollideWithPlayer_Transpiler could not use id player local or intern");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("MainSnakeActAsConductor")]
        [HarmonyPostfix]
        static void MainSnakeActAsConductor_PostFix(FlowerSnakeEnemy __instance,
                                                    ref Vector3 ___forces)
        {
            if (__instance.clingingToPlayer == null)
            {
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.clingingToPlayer.playerClientId);
            if (internAI == null)
            {
                return;
            }

            if ((__instance.clingingToPlayer.isInElevator && StartOfRound.Instance.shipIsLeaving)
                || __instance.clingingToPlayer.inAnimationWithEnemy != null
                || __instance.clingToPlayerTimer <= 0f
                || __instance.daytimeEnemyLeaving)
            {
                int clingingPlayerID = (int)__instance.clingingToPlayer.playerClientId;
                __instance.StopClingingOnLocalClient(true);
                __instance.StopClingingServerRpc(clingingPlayerID);
                return;
            }
            if (__instance.activatedFlight)
            {
                __instance.flightPower = Mathf.Clamp(__instance.flightPower + Time.deltaTime * 1.7f, 12f, (float)__instance.clingingToPlayer.enemiesOnPerson * 4f);
            }
            else
            {
                __instance.flightPower = Mathf.Clamp(__instance.flightPower - Time.deltaTime * 50f, 0f, 1000f);
                if (internAI.NpcController.IsTouchingGround)
                {
                    __instance.flightPower = 0f;
                }
            }
            ___forces = Vector3.Lerp(___forces, Vector3.ClampMagnitude(__instance.clingingToPlayer.transform.up * __instance.flightPower, 400f), Time.deltaTime);
            if (!__instance.clingingToPlayer.jetpackControls)
            {
                ___forces = Vector3.zero;
            }
            __instance.clingingToPlayer.externalForces += ___forces;
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
            if (InternManager.Instance.IsIdPlayerInternOwnerLocal(playerId)
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
