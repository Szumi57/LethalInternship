using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TooManyEmotes;

namespace LethalInternship.Patches.ModPatches.TooManyEmotes
{
    [HarmonyPatch(typeof(EmoteControllerPlayer))]
    public class EmoteControllerPlayerPatch
    {
        [HarmonyPatch("PerformEmote", new Type[] { typeof(UnlockableEmote), typeof(int), typeof(bool) })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PerformEmote_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("call static void TooManyEmotes.Patches.ThirdPersonEmoteController::OnStartCustomEmoteLocal()") // 94
                    && codes[i + 2].ToString().StartsWith("nop NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 2].labels.Add(labelToJumpTo);
                // ldfld GameNetcodeStuff.PlayerControllerB TooManyEmotes.EmoteControllerPlayer::playerController
                CodeInstruction loadFieldPlayerController = new CodeInstruction(codes[2].opcode, codes[2].operand);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    loadFieldPlayerController,
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.TooManyEmotes.EmoteControllerPlayerPatch.PerformEmote_Transpiler could not not do OnStartCustomEmoteLocal when intern.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("StopPerformingEmote")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StopPerformingEmote_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString().StartsWith("call static void TooManyEmotes.Patches.ThirdPersonEmoteController::OnStopCustomEmoteLocal()") // 94
                    && codes[i + 2].ToString().StartsWith("ldarg.0 NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 2].labels.Add(labelToJumpTo);
                // ldfld GameNetcodeStuff.PlayerControllerB TooManyEmotes.EmoteControllerPlayer::playerController
                CodeInstruction loadFieldPlayerController = new CodeInstruction(codes[2].opcode, codes[2].operand);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    loadFieldPlayerController,
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.ModPatches.TooManyEmotes.EmoteControllerPlayerPatch.StopPerformingEmote_Transpiler could not not do OnStopCustomEmoteLocal when intern.");
            }

            return codes.AsEnumerable();
        }
    }
}
