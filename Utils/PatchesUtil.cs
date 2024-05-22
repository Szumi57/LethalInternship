using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace LethalInternship.Utils
{
    internal static class PatchesUtil
    {
        static readonly MethodInfo IsPlayerInternMethod = SymbolExtensions.GetMethodInfo(() => PatchesUtil.IsPlayerIntern(new PlayerControllerB()));

        public static bool IsColliderFromIntern(Collider collider)
        {
            PlayerControllerB player = collider.gameObject.GetComponent<PlayerControllerB>();
            return IsPlayerIntern(player);
        }

        public static bool IsPlayerIntern(PlayerControllerB player)
        {
            InternAI? internAI = StartOfRoundPatch.GetInternAI((int)player.playerClientId);
            return internAI != null;
        }

        public static List<CodeInstruction> InsertIsPlayerInternInstructions(List<CodeInstruction> codes,
                                                                             ILGenerator generator,
                                                                             int startIndex,
                                                                             int indexToJumpTo)
        {
            Label labelToJumpTo;
            List<Label> labelsOfStartCode = codes[startIndex].labels;
            List<Label> labelsOfCodeToJumpTo = codes[startIndex + indexToJumpTo].labels;
            List<CodeInstruction> codesToAdd;

            // Define label for the jump
            labelToJumpTo = generator.DefineLabel();
            labelsOfCodeToJumpTo.Add(labelToJumpTo);

            // Rearrange label if start is a destination label for a previous code
            if (labelsOfStartCode.Count > 0)
            {
                codes.Insert(startIndex + 1, new CodeInstruction(codes[startIndex].opcode, codes[startIndex].operand));
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                startIndex++;
            }

            codesToAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, IsPlayerInternMethod),
                new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
            };
            codes.InsertRange(startIndex, codesToAdd);
            return codes;
        }

        public static List<CodeInstruction> InsertLogOfFieldOfThis(string logWithZeroParameter, FieldInfo fieldInfo, Type fieldType)
        {
            return new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "Logger")),
                    new CodeInstruction(OpCodes.Ldstr, logWithZeroParameter),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, fieldInfo),
                    new CodeInstruction(OpCodes.Box, fieldType),
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => String.Format(new String(new char[]{ }), new object()))),
                    new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => Plugin.Logger.LogDebug(new object()))),
                };

            //codes.InsertRange(0, PatchesUtil.InsertLogOfFieldOfThis("isPlayerControlled {0}", AccessTools.Field(typeof(PlayerControllerB), "isPlayerControlled"), typeof(bool)));
        }

        public static List<CodeInstruction> InsertLogWithoutParameters(string log)
        {
            return new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "Logger")),
                    new CodeInstruction(OpCodes.Ldstr, log),
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => String.Format(new String(new char[]{ })))),
                    new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => Plugin.Logger.LogDebug(new object()))),
                };

            //codes.InsertRange(0, PatchesUtil.InsertLogOfFieldOfThis("isPlayerControlled {0}", AccessTools.Field(typeof(PlayerControllerB), "isPlayerControlled"), typeof(bool)));
        }
    }
}
