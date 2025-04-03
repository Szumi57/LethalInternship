using HarmonyLib;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatch
    {
        [HarmonyPatch("FinishKillAnimation")]
        [HarmonyPrefix]
        static bool FinishKillAnimation_PreFix(MaskedPlayerEnemy __instance)
        {
            if (__instance.inSpecialAnimationWithPlayer == null)
            {
                return true;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.inSpecialAnimationWithPlayer.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (internAI.NpcController.EnemyInAnimationWith == __instance)
            {
                internAI.NpcController.EnemyInAnimationWith = null;
            }
            return true;
        }

        [HarmonyPatch("KillPlayerAnimationClientRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerAnimationClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 2].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)") //
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
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.MaskedPlayerEnemyPatch.KillPlayerAnimationClientRpc_Transpiler could not check if player local or intern local 1");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 2].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)") //
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
                Plugin.LogError($"LethalInternship.Patches.EnemiesPatches.MaskedPlayerEnemyPatch.KillPlayerAnimationClientRpc_Transpiler could not check if player local or intern local 2");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("KillPlayerAnimationClientRpc")]
        [HarmonyPostfix]
        static void KillPlayerAnimationClientRpc_PostFix(MaskedPlayerEnemy __instance)
        {
            if (__instance.inSpecialAnimationWithPlayer == null)
            {
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.inSpecialAnimationWithPlayer.playerClientId);
            if (internAI == null)
            {
                return;
            }

            internAI.NpcController.EnemyInAnimationWith = __instance;
        }
    }
}
