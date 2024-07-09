using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    /// <summary>
    /// Patch for <c>InteractTrigger</c>
    /// </summary>
    [HarmonyPatch(typeof(InteractTrigger))]
    internal class InteractTriggerPatch
    {
        /// <summary>
        /// Patch for not making the intern able to cancel the ladder animation of a player already on the ladder 
        /// </summary>
        /// <remarks>
        /// Behaviour still can't fully understand, not more than one player on the ladder ? can/should a player cancel another player on ladder ? not clear
        /// </remarks>
        /// <param name="instance"></param>
        /// <param name="playerTransform"></param>
        [HarmonyPatch("Interact")]
        [HarmonyReversePatch]
        public static void Interact_ReversePatch(object instance, Transform playerTransform)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //36
                        && codes[i + 1].ToString() == "call void InteractTrigger::CancelLadderAnimation()")
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
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.MapPatches.InteractTriggerPatch.Interact_ReversePatch could not remove CancelLadderAnimation");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
