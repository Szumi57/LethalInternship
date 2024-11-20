using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for <c>ForestGiantAI</c>
    /// </summary>
    [HarmonyPatch(typeof(ForestGiantAI))]
    internal class ForestGiantAIPatch
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
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //31
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 2].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)") //49
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
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.ForestGiantAIPatch.OnCollideWithPlayer_Transpiler could not check if player local or intern");
            }

            // ----------------------------------------------------------------------
            // Replace on all occurences localPlayerController by the player from getComponent just before, so the player is local or intern
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController")
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                    codes[i + 1].opcode = OpCodes.Ldloc_0;
                    codes[i + 1].operand = null;
                }
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch for initialize playerStealthMeters for the right amount of player + interns
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("LookForPlayers")]
        [HarmonyPrefix]
        [HarmonyAfter(Const.MORECOMPANY_GUID)]
        static void LookForPlayers_Prefix(ref ForestGiantAI __instance)
        {
            InternManager manager = InternManager.Instance;
            if (__instance.playerStealthMeters.Length != manager.AllEntitiesCount)
            {
                Array.Resize(ref __instance.playerStealthMeters, manager.AllEntitiesCount);
                for (int i = 0; i < manager.AllEntitiesCount; i++)
                {
                    __instance.playerStealthMeters[i] = 0f;
                }
            }
        }
    }
}
