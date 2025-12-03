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

            for (int i = 0; i < codes.Count; i++)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{i} {codes[i]}");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0") // 72
                    && codes[i + 6].ToString().StartsWith("call UnityEngine.Transform UnityEngine.RaycastHit::get_transform")) // 78
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
                    //new CodeInstruction(codes[startIndex]), // ldarg.0 NULL
                    //new CodeInstruction(codes[startIndex+1]), // ldfld System.Collections.Generic.List<UnityEngine.RaycastHit> Shovel::objectsHitByShovelList
                    //new CodeInstruction(codes[startIndex+2]), // ldloc.s 7
                    //new CodeInstruction(codes[startIndex+3]), // callvirt virtual UnityEngine.RaycastHit System.Collections.Generic.List<UnityEngine.RaycastHit>::get_Item(int index)
                    //new CodeInstruction(codes[startIndex+4]), // stloc.s 8
                    //new CodeInstruction(codes[startIndex+5]), // ldloca.s 8
                    //new CodeInstruction(codes[startIndex+6]), // call UnityEngine.Transform UnityEngine.RaycastHit::get_transform()
                    new CodeInstruction(OpCodes.Call, PatchesUtil.ShouldShovelIgnoreInternMethod),
                    //new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + (285 - 72)].labels[0]) // 285
                    new CodeInstruction(OpCodes.Brtrue_S, codes[346].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ObjectsPatches.ShovelPatch.HitShovel_Transpiler could not ");
            }

            for (int i = 0; i < codes.Count; i++)
            {
                PluginLoggerHook.LogDebug?.Invoke($"+{i} {codes[i]}");
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
