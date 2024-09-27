using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches.BetterEmotes
{
    internal class BetterEmotesPatch
    {
        public static void UpdatePrefix_Prefix(ref bool[] ___playersPerformingEmotes)
        {
            int allEntitiesCount = InternManager.Instance.AllEntitiesCount;
            if (___playersPerformingEmotes != null
                && ___playersPerformingEmotes.Length < allEntitiesCount)
            {
                Array.Resize(ref ___playersPerformingEmotes, allEntitiesCount);
            }
        }

        public static IEnumerable<CodeInstruction> UpdatePostfix_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = 0;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            Label labelToJumpTo = generator.DefineLabel();
            codes[startIndex + 32].labels.Add(labelToJumpTo);

            // Use runtimeAnimatorController local for interns owned by player local
            List<CodeInstruction> codesToAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                // BetterEmotes mod need method kust is intern but not BetterEmotes mod, I don't know why
                new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
            };
            codes.InsertRange(startIndex, codesToAdd);

            return codes.AsEnumerable();
        }

        public static IEnumerable<CodeInstruction> PerformEmotePrefix_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            // ldfld GameNetcodeStuff.PlayerControllerB BetterEmote.Patches.EmotePatch+<>c__DisplayClass12_0::__instance
            CodeInstruction codeInstructionLoadInstance = new CodeInstruction(codes[6].opcode, codes[6].operand);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 24; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0") // 33
                    && codes[i + 2].ToString().StartsWith("callvirt bool Unity.Netcode.NetworkBehaviour::get_IsOwner()")
                    && codes[i + 24].ToString().StartsWith("ldsfld BetterEmote.Netcode.SyncVRState BetterEmote.Patches.EmotePatch::syncVR")) // 57
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 24].labels.Add(labelToJumpTo); // 57

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
                Plugin.LogError($"LethalInternship.Patches.ModPatches.BetterEmotesPatch.PerformEmotePrefix_Transpiler could not use irl number of player for iteration.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0") // 95
                    && codes[i + 1].ToString().StartsWith("call bool UnityEngine.InputSystem.InputAction+CallbackContext::get_performed()"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                // ldtoken BetterEmote.Utils.DoubleEmote
                codes[startIndex + 7].labels.Add(labelToJumpTo); // 102

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
                Plugin.LogError($"LethalInternship.Patches.ModPatches.BetterEmotesPatch.PerformEmotePrefix_Transpiler could not bypass context performed for interns.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("ldloc.0") // 291
                    && codes[i + 1].ToString().StartsWith("ldfld GameNetcodeStuff.PlayerControllerB")
                    && codes[i + 2].ToString().StartsWith("ldfld float GameNetcodeStuff.PlayerControllerB::timeSinceStartingEmote")) // 293
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 5].labels.Add(labelToJumpTo); // 296

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
                Plugin.LogError($"LethalInternship.Patches.ModPatches.BetterEmotesPatch.PerformEmotePrefix_Transpiler could not bypass timeSinceStartingEmote for interns.");
            }

            return codes.AsEnumerable();
        }
    }
}
