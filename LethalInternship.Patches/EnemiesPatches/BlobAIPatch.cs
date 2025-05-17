using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for the <c>BlobAI</c>
    /// </summary>
    [HarmonyPatch(typeof(BlobAI))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    public class BlobAIPatch
    {
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnCollideWithPlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0 NULL") // 30
                    && codes[i + 1].ToString().StartsWith("ldc.i4.s 35"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt, PatchesUtil.GetDamageFromSlimeIfInternMethod),
                };
                codes.InsertRange(startIndex + 1, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.BlobAIPatch.OnCollideWithPlayer_Transpiler could not change default damage to intern.");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch the numbers of ragdoll colliders of the <c>BlobAI</c>
        /// </summary>
        /// <param name="___ragdollColliders"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref Collider[] ___ragdollColliders)
        {
            ___ragdollColliders = new Collider[InternManagerProvider.Instance.AllEntitiesCount];
        }
    }
}
