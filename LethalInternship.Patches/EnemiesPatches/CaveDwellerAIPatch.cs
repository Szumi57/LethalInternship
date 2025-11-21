using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(CaveDwellerAI))]
    public class CaveDwellerAIPatch
    {
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static void OnCollideWithPlayer_PreFix(ref bool ___startingKillAnimationLocalClient)
        {
            // startingKillAnimationLocalClient mysteriously set back to true after killing intern... force it to false here
            // Maybe bugs will occurs we'll see
            ___startingKillAnimationLocalClient = false;
        }


        [HarmonyPatch("ScareBaby")]
        [HarmonyPostfix]
        static void ScareBaby_PostFix(CaveDwellerAI __instance)
        {
            if (!__instance.IsServer)
            {
                return;
            }

            if (__instance.sittingDown && !__instance.holdingBaby)
            {
                return;
            }

            if (__instance.propScript.playerHeldBy == null)
            {
                return;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.propScript.playerHeldBy.playerClientId);
            if (internAI == null)
            {
                return;
            }

            PluginLoggerHook.LogDebug?.Invoke("ScareBaby_PostFix");
            internAI.DropLastPickedUpItem();
        }

        [HarmonyPatch("ScareBabyClientRpc")]
        [HarmonyPostfix]
        static void ScareBabyClientRpc_PostFix(CaveDwellerAI __instance)
        {
            if (__instance.IsServer)
            {
                return;
            }

            if (__instance.propScript.playerHeldBy == null)
            {
                return;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.propScript.playerHeldBy.playerClientId);
            if (internAI == null)
            {
                return;
            }

            PluginLoggerHook.LogDebug?.Invoke("ScareBabyClientRpc_PostFix");
            internAI.DropLastPickedUpItem();
        }

        [HarmonyPatch("CancelKillAnimationClientRpc")]
        [HarmonyPostfix]
        static void CancelKillAnimationClientRpc_PostFix(int playerObjectId,
                                                         ref bool ___startingKillAnimationLocalClient)
        {
            PluginLoggerHook.LogDebug?.Invoke($"CancelKillAnimationClientRpc_PostFix playerObjectId {playerObjectId}");
            if (InternManagerProvider.Instance.IsIdPlayerInternOwnerLocal(playerObjectId))
            {
                PluginLoggerHook.LogDebug?.Invoke("CancelKillAnimationClientRpc_PostFix");
                ___startingKillAnimationLocalClient = false;
            }
        }

        [HarmonyPatch("KillPlayerAnimationClientRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerAnimationClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0 NULL")
                    && codes[i + 1].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 3].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Nop;
                codes[startIndex + 2].operand = null;
                codes[startIndex + 3].opcode = OpCodes.Call;
                codes[startIndex + 3].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.CaveDwellerAIPatch.KillPlayerAnimationClientRpc_Transpiler could not check if local player or intern");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("StartTransformationAnim")]
        [HarmonyPostfix]
        static void StartTransformationAnim_PostFix(CaveDwellerAI __instance)
        {
            if (__instance.propScript.playerHeldBy == null)
            {
                return;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.propScript.playerHeldBy.playerClientId);
            if (internAI == null)
            {
                return;
            }

            internAI.DropLastPickedUpItem();
        }
    }
}
