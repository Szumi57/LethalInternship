using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(Shovel))]
    public class ShovelPatch
    {
        [HarmonyPatch("HitShovel")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HitShovel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 28; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0") // 22
                    && codes[i + 28].ToString().StartsWith("call static UnityEngine.RaycastHit[] UnityEngine.Physics::SphereCastAll")) // 50
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(codes[startIndex]), // ldarg.0 NULL (this: Shovel)
                    new CodeInstruction(OpCodes.Call, PatchesUtil.ShouldIgnoreIfInternMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[346].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ObjectsPatches.ShovelPatch.HitShovel_Transpiler could not ignore shovel hit if holder is intern");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("HitShovel")]
        [HarmonyPostfix]
        public static void HitShovel_PostFix(Shovel __instance,
                                             bool cancel)
        {
            if (cancel)
            {
                return;
            }

            IInternAI? internHolder = InternManagerProvider.Instance.GetInternAI((int)__instance.playerHeldBy.playerClientId);
            if (internHolder == null)
            {
                return;
            }

            internHolder.HitTargetWithShovel(__instance);
        }
    }
}
