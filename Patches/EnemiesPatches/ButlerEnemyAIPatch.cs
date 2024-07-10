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
    /// Patches for <c>ButlerEnemyAI</c>
    /// </summary>
    [HarmonyPatch(typeof(ButlerEnemyAI))]
    internal class ButlerEnemyAIPatch
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
                if (codes[i].ToString() == "ldloc.0 NULL" //44
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController") //46
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
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.ButlerEnemyAIPatch.OnCollideWithPlayer_Transpiler could not check if local player or intern owner local player");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Initiate correctly to account for the number of players and interns
        /// </summary>
        /// <param name="___lastSeenPlayerPositions"></param>
        /// <param name="___seenPlayers"></param>
        /// <param name="___timeOfLastSeenPlayers"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_Postfix(ref Vector3[] ___lastSeenPlayerPositions,
                                  ref bool[] ___seenPlayers,
                                  ref float[] ___timeOfLastSeenPlayers)
        {
            ___lastSeenPlayerPositions = new Vector3[InternManager.Instance.AllEntitiesCount];
            ___seenPlayers = new bool[InternManager.Instance.AllEntitiesCount];
            ___timeOfLastSeenPlayers = new float[InternManager.Instance.AllEntitiesCount];
        }
    }
}
