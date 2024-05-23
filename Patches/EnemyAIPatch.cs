using GameNetcodeStuff;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using LethalInternship.AI;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        static MethodInfo IsColliderFromIntern = SymbolExtensions.GetMethodInfo(() => PatchesUtil.IsColliderFromIntern(new Collider()));

        [HarmonyPatch("ChangeOwnershipOfEnemy")]
        [HarmonyPrefix]
        static bool ChangeOwnershipOfEnemy_PreFix(ref ulong newOwnerClientId)
        {
            //todo !! ActualClientId not localClientId
            Plugin.Logger.LogDebug($"ChangeOwnershipOfEnemy index {(int)newOwnerClientId}");
            InternAI? internAI = InternManager.GetInternAI((int)newOwnerClientId);
            if (internAI != null)
            {
                // do not change owner on an intern
                if (internAI.targetPlayer != null)
                {
                    Plugin.Logger.LogDebug($"ChangeOwnershipOfEnemy not on intern but on {internAI.targetPlayer.playerClientId}");
                    newOwnerClientId = internAI.targetPlayer.playerClientId;
                    return true;
                }
                else
                {
                    Plugin.Logger.LogDebug($"Try to change ownership on {newOwnerClientId}");
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch("MeetsStandardPlayerCollisionConditions")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MeetsStandardPlayerCollisionConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // bypass "component != GameNetworkManager.Instance.localPlayerController" if player is an intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].opcode == OpCodes.Brtrue
                    && codes[i + 1].opcode == OpCodes.Ldloc_0
                    && codes[i + 2].opcode == OpCodes.Call
                    && codes[i + 3].opcode == OpCodes.Ldfld
                    && codes[i + 4].opcode == OpCodes.Call
                    && codes[i + 8].opcode == OpCodes.Ldarg_0)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, IsColliderFromIntern),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + 8].labels.First()/*IL_0051*/)
                };
                codes.InsertRange(startIndex + 1, codesToAdd);
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemyAIPatch.MeetsStandardPlayerCollisionConditions_Transpiler could not insert instruction if is intern for \"component != GameNetworkManager.Instance.localPlayerController\".");
            }

            return codes.AsEnumerable();
        }
    }
}
