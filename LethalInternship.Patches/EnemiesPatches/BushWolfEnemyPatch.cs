using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>BushWolfEnemy</c>
    /// </summary>
    [HarmonyPatch(typeof(BushWolfEnemy))]
    public class BushWolfEnemyPatch
    {
        /// <summary>
        /// Patch for making the bush wolf be able to kill an intern
        /// </summary>
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPostfix]
        static void OnCollideWithPlayer_PostFix(BushWolfEnemy __instance,
                                                Collider other,
                                                bool ___foundSpawningPoint,
                                                bool ___inKillAnimation,
                                                Vector3 ___currentHidingSpot,
                                                float ___timeSinceTakingDamage,
                                                PlayerControllerB ___lastHitByPlayer,
                                                bool ___dragging,
                                                bool ___startedShootingTongue)
        {
            if (!___foundSpawningPoint)
            {
                return;
            }
            if (___inKillAnimation)
            {
                return;
            }
            if (__instance.isEnemyDead)
            {
                return;
            }

            PlayerControllerB playerController = __instance.MeetsStandardPlayerCollisionConditions(other, ___inKillAnimation, false);
            if (playerController == null)
            {
                return;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI == null)
            {
                return;
            }

            float num = Vector3.Distance(__instance.transform.position, ___currentHidingSpot);
            bool flag = false;
            if (___timeSinceTakingDamage < 2.5f && ___lastHitByPlayer != null && num < 16f)
            {
                flag = true;
            }
            else if (num < 7f && ___dragging && !___startedShootingTongue && __instance.targetPlayer == playerController)
            {
                flag = true;
            }
            if (flag)
            {
                playerController.KillPlayer(Vector3.up * 15f, spawnBody: true, CauseOfDeath.Mauling, 8, default);
                __instance.DoKillPlayerAnimationServerRpc((int)__instance.targetPlayer.playerClientId);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ------------------------------------------------
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].ToString().StartsWith("callvirt void GameNetcodeStuff.PlayerControllerB::CancelSpecialTriggerAnimations")) // 1646
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.DropAllItemsIfInternMethod),
                };
                //-----------------------------
                codes.InsertRange(startIndex + 26/*1672*/, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not drop all items if dragging intern");
            }


            // ------------------------------------------------
            for (var i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0") // 2229
                    && codes[i + 1].ToString().StartsWith("ldfld float BushWolfEnemy::shootTongueTimer") // 2230
                    && codes[i + 5].ToString().StartsWith("call void BushWolfEnemy::TongueShootWasUnsuccessful(")) // 2234
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.BushWolfEnemyCheckIfHitInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, codes[startIndex + 6].labels.First()) // exit to 2235
                };
                //-----------------------------
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if tongue hit intern");
            }

            return codes.AsEnumerable();
        }
    }
}