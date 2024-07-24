using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Label = System.Reflection.Emit.Label;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>BushWolfEnemy</c>
    /// </summary>
    [HarmonyPatch(typeof(BushWolfEnemy))]
    internal class BushWolfEnemyPatch
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

            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI == null)
            {
                return;
            }

            Plugin.LogDebug($"fox saw intern #{internAI.InternId}");
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
                internAI.SyncKillIntern(Vector3.up * 15f, true, CauseOfDeath.Mauling, 8, default);
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
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 1589
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB EnemyAI::targetPlayer") // 1591
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)") // 1592
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 1593
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 11].labels.First()) // br to 1600
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 1");
            }

            // -----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 1604
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB BushWolfEnemy::draggingPlayer") // 1606
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)") // 1607
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 1608
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoDraggingPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 10].labels.First()) // br to 1614
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 2");
            }

            // -----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 1681
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB EnemyAI::targetPlayer") // 1683
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 1685
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 331].labels.First()) // br to 2012
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 3");
            }

            // -----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("call static GameNetworkManager GameNetworkManager::get_Instance()") // 1686
                    && codes[i + 1].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 1687
                    && codes[i + 4].ToString().StartsWith("callvirt void GameNetcodeStuff.PlayerControllerB::JumpToFearLevel(")) // 1690
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Ldarg_0;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldfld;
                codes[startIndex + 1].operand = PatchesUtil.FieldInfoTargetPlayer;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not use target player for JumpToFearLevel method");
            }

            // ------------------------------------------------ (upperSpineLocalPoint 1)
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 2036
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB EnemyAI::targetPlayer")
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 2040
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 11].labels.First()) // br to 2047
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 4");
            }

            // ------------------------------------------------ (upperSpineLocalPoint 2)
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") // 2098
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB EnemyAI::targetPlayer")
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 2012
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 11].labels.First()) // br to 2109
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 5");
            }

            // ------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB EnemyAI::targetPlayer") // 2118
                    && codes[i + 2].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController")
                    && codes[i + 3].ToString().StartsWith("call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                    && codes[i + 4].ToString().StartsWith("brfalse")) // 2122
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // If is localPlayerController
                Label label = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(label);

                codes[startIndex + 4].opcode = OpCodes.Brtrue;
                codes[startIndex + 4].operand = label;
                //---------------------------
                // or
                // If is intern owned by localPlayerController
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoTargetPlayer),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brfalse, codes[startIndex + 39].labels.First()) // br to 2157
                };
                //-----------------------------
                codes.InsertRange(startIndex + 5, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not check if intern or local player 6");
            }

            // -----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 18; i++)
            {
                if (codes[i].ToString().StartsWith("call static GameNetworkManager GameNetworkManager::get_Instance()") // 2126
                    && codes[i + 1].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController")
                    && codes[i + 17].ToString().StartsWith("call static GameNetworkManager GameNetworkManager::get_Instance()") // 2143
                    && codes[i + 18].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Ldarg_0;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldfld;
                codes[startIndex + 1].operand = PatchesUtil.FieldInfoTargetPlayer;

                codes[startIndex + 17].opcode = OpCodes.Ldarg_0;
                codes[startIndex + 17].operand = null;
                codes[startIndex + 18].opcode = OpCodes.Ldfld;
                codes[startIndex + 18].operand = PatchesUtil.FieldInfoTargetPlayer;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.BushWolfEnemyPatch.Update_Transpiler could not use target player for check if HitByEnemyServerRpc method");
            }

            return codes.AsEnumerable();
        }
    }
}