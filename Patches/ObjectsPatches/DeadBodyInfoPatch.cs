using HarmonyLib;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ObjectsPatches
{
    /// <summary>
    /// Patches for <c>DeadBodyInfo</c>
    /// </summary>
    [HarmonyPatch(typeof(DeadBodyInfo))]
    public class DeadBodyInfoPatch
    {
        [HarmonyPatch("DetectIfSeenByLocalPlayer")]
        [HarmonyPrefix]
        static bool DetectIfSeenByLocalPlayer_PreFix(DeadBodyInfo __instance)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerObjectId);
            if (internAI != null
                && internAI.RagdollInternBody != null
                && internAI.RagdollInternBody.GetDeadBodyInfo() == __instance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch for assigning right tag to a dead body for not getting debug logs of errors
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("Start")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Start_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //65
                    && codes[i + 3].ToString().StartsWith("ldarg.0 NULL")//68
                    && codes[i + 8].ToString() == "ldstr \"PlayerRagdoll\"")//73
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<Label> labelsOfCodeToJumpTo = codes[startIndex + 3].labels;

                // Define label for the jump
                Label labelToJumpTo = generator.DefineLabel();
                labelsOfCodeToJumpTo.Add(labelToJumpTo);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                                                        {
                                                            new CodeInstruction(OpCodes.Ldarg_0, null),
                                                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DeadBodyInfo), "playerObjectId")),
                                                            new CodeInstruction(OpCodes.Call, PatchesUtil.IsIdPlayerInternMethod),
                                                            new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
                                                        };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ObjectsPatches.DeadBodyInfoPatch.Start_Transpiler remplace with correct tag if intern.");
            }

            // ----------------------------------------------------------------------
            return codes.AsEnumerable();
        }
    }
}
