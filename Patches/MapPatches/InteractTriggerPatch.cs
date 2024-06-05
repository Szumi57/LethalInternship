using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(InteractTrigger))]
    internal class InteractTriggerPatch
    {
        [HarmonyPatch("Interact")]
        [HarmonyPrefix]
        static bool Interact_PreFix(InteractTrigger __instance)
        {
            if (!__instance.interactable || __instance.isPlayingSpecialAnimation || __instance.usingLadder)
            {
                if (__instance.usingLadder)
                {
                    Plugin.Logger.LogDebug("why cancel ????");
                }
            }

            return true;
        }

        [HarmonyPatch("Interact")]
        [HarmonyReversePatch]
        public static void Interact_ReversePatch(object instance, Transform playerTransform)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //Plugin.Logger.LogDebug($"Interact ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchBeginGrabObject_ReversePatch could not remove hitray condition");
                }

                //Plugin.Logger.LogDebug($"Interact ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"Interact ======================");
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("CancelLadderAnimation")]
        [HarmonyPostfix]
        static void CancelLadderAnimation_PostFix(InteractTrigger __instance)
        {
            Plugin.Logger.LogDebug("wtf!!!!!");
        }
    }
}
