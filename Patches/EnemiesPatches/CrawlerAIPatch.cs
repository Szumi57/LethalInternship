using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for <c>CrawlerAI</c>
    /// </summary>
    [HarmonyPatch(typeof(CrawlerAI))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class CrawlerAIPatch
    {
        /// <summary>
        /// Patch fields to take into account for the numbers of players + interns
        /// </summary>
        /// <param name="___nearPlayerColliders"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref Collider[] ___nearPlayerColliders)
        {
            ___nearPlayerColliders = new Collider[InternManager.Instance.AllEntitiesCount];
        }

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
            for (var i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL"//36
                    && codes[i + 1].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//37
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 5].ToString() == "call void CrawlerAI::HitPlayerServerRpc(int playerId)")//41
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
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.CrawlerAIPatch.OnCollideWithPlayer_Transpiler could not change use of correct player id for HitPlayerServerRpc.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//42
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 4].ToString() == "callvirt void GameNetcodeStuff.PlayerControllerB::JumpToFearLevel(float targetFearLevel, bool onlyGoUp)")//46
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
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.CrawlerAIPatch.OnCollideWithPlayer_Transpiler could not change use of correct player id for JumpToFearLevel.");
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

        /// <summary>
        /// Patch udpate to make the enemy target intern too if possible
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldloc.1 NULL [Label11]"//120
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"//122
                    && codes[i + 3].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")//123
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2] = new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod);
                codes[startIndex + 3].opcode = OpCodes.Nop; // op_Equality
                codes[startIndex + 3].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.CrawlerAIPatch.Update_Transpiler could not change condition for is only local player.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"//145
                    && codes[i + 4].ToString() == "call void CrawlerAI::BeginChasingPlayerServerRpc(int playerObjectId)")//149
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Ldloc_1;
                codes[startIndex + 1].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.CrawlerAIPatch.Update_Transpiler could not change target of begin chasing player.");
            }

            return codes.AsEnumerable();
        }
    }
}



