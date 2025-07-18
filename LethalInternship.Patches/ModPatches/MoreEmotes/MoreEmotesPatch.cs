using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.ManagerProviders;
using MoreEmotes.Patch;
using MoreEmotes.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches.MoreEmotes
{
    [HarmonyPatch(typeof(EmotePatch))]
    public class MoreEmotesPatch
    {
        [HarmonyPatch("StartPostfix")]
        [HarmonyPrefix]
        public static bool StartPostfix_Prefix(PlayerControllerB __0)
        {
            if (__0.gameObject.GetComponent<CustomAnimationObjects>() != null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("UpdatePrefix")]
        [HarmonyPrefix]
        static void UpdatePrefix_Prefix(ref bool[] ___s_wasPerformingEmote)
        {
            int allEntitiesCount = InternManagerProvider.Instance.AllEntitiesCount;
            if (___s_wasPerformingEmote != null
                && ___s_wasPerformingEmote.Length < allEntitiesCount)
            {
                Array.Resize(ref ___s_wasPerformingEmote, allEntitiesCount);
            }
        }

        [HarmonyPatch("UpdatePostfix")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UpdatePostfix_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = 0;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            Label labelToJumpTo = generator.DefineLabel();
            codes[startIndex + 25].labels.Add(labelToJumpTo);

            // Use runtimeAnimatorController local for interns owned by player local
            List<CodeInstruction> codesToAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternOwnerLocalMethod),
                new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
            };
            codes.InsertRange(startIndex, codesToAdd);

            return codes.AsEnumerable();
        }

        [HarmonyPatch("PerformEmotePrefix")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PerformEmotePrefix_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            // ldfld GameNetcodeStuff.PlayerControllerB MoreEmotes.Patch.EmotePatch+<>c__DisplayClass61_0::__instance
            CodeInstruction codeInstructionLoadInstance = new CodeInstruction(codes[14].opcode, codes[14].operand);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 29; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0") // 29
                    && codes[i + 2].ToString().StartsWith("callvirt bool Unity.Netcode.NetworkBehaviour::get_IsOwner()")
                    && codes[i + 29].ToString().StartsWith("ldsfld MoreEmotes.Scripts.SignUI MoreEmotes.Patch.EmotePatch::s_customSignInputField")) // 58
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 29].labels.Add(labelToJumpTo); // 58

                // Adds dummy line for label that land here
                codes.Insert(startIndex + 1, new CodeInstruction(codes[startIndex].opcode, codes[startIndex].operand));
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                startIndex++;

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    codeInstructionLoadInstance,
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternControlledAndOwnerMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.MoreEmotesPatch.PerformEmotePrefix_Transpiler could not use irl number of player for iteration.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString().StartsWith("ldarga.s 0") // 92
                    && codes[i + 1].ToString().StartsWith("call bool UnityEngine.InputSystem.InputAction+CallbackContext::get_performed()")) //
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 10].labels.Add(labelToJumpTo); // 102

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    codeInstructionLoadInstance,
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.MoreEmotesPatch.PerformEmotePrefix_Transpiler could not bypass context performed for interns.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0") // 224
                    && codes[i + 1].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB MoreEmotes.Patch.EmotePatch+<>c__DisplayClass61_0::__instance")
                    && codes[i + 2].ToString().StartsWith("ldfld float GameNetcodeStuff.PlayerControllerB::timeSinceStartingEmote")) // 226
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 11].labels.Add(labelToJumpTo); // 235

                // Adds dummy line for label that land here
                codes.Insert(startIndex + 1, new CodeInstruction(codes[startIndex].opcode, codes[startIndex].operand));
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                startIndex++;

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    codeInstructionLoadInstance,
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.MoreEmotesPatch.PerformEmotePrefix_Transpiler could not bypass timeSinceStartingEmote for interns.");
            }

            return codes.AsEnumerable();
        }
    }
}
