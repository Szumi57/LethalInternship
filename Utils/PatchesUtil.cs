using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Utils
{
    internal static class PatchesUtil
    {
        public static readonly FieldInfo FieldInfoWasUnderwaterLastFrame = AccessTools.Field(typeof(PlayerControllerB), "wasUnderwaterLastFrame");
        public static readonly FieldInfo FieldInfoPlayerClientId = AccessTools.Field(typeof(PlayerControllerB), "playerClientId");
        public static readonly FieldInfo FieldInfoPreviousAnimationStateHash = AccessTools.Field(typeof(PlayerControllerB), "previousAnimationStateHash");
        public static readonly FieldInfo FieldInfoCurrentAnimationStateHash = AccessTools.Field(typeof(PlayerControllerB), "currentAnimationStateHash");
        public static readonly FieldInfo FieldInfoAllEntitiesCount = AccessTools.Field(typeof(InternManager), "AllEntitiesCount");

        public static readonly MethodInfo AreInternsScheduledToLandMethod = SymbolExtensions.GetMethodInfo(() => AreInternsScheduledToLand());
        public static readonly MethodInfo IsPlayerLocalOrInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerLocalOrInternOwnerLocal(new PlayerControllerB()));
        public static readonly MethodInfo IsColliderFromLocalOrInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsColliderFromLocalOrInternOwnerLocal(new Collider()));
        public static readonly MethodInfo IndexBeginOfInternsMethod = SymbolExtensions.GetMethodInfo(() => IndexBeginOfInterns());
        public static readonly MethodInfo IsIdPlayerInternMethod = SymbolExtensions.GetMethodInfo(() => IsIdPlayerIntern(new int()));
        public static readonly MethodInfo IsPlayerInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerInternOwnerLocal(new PlayerControllerB()));
        public static readonly MethodInfo DisableOriginalGameDebugLogsMethod = SymbolExtensions.GetMethodInfo(() => DisableOriginalGameDebugLogs());

        public static readonly MethodInfo UpdatePlayerAnimationServerRpcMethod = SymbolExtensions.GetMethodInfo(() => UpdatePlayerAnimationServerRpc(new ulong(), new int(), new int()));
        public static readonly MethodInfo SyncJumpMethod = SymbolExtensions.GetMethodInfo(() => SyncJump(new ulong()));
        public static readonly MethodInfo SyncLandFromJumpMethod = SymbolExtensions.GetMethodInfo(() => SyncLandFromJump(new ulong(), new bool()));

        private static readonly MethodInfo IsPlayerInternMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerIntern(new PlayerControllerB()));

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
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => String.Format(new string(new char[]{ }), new object()))),
                    new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => Plugin.LogDebug(new string(new char[]{ })))),
                };

            //codes.InsertRange(0, PatchesUtil.InsertLogOfFieldOfThis("isPlayerControlled {0}", AccessTools.Field(typeof(PlayerControllerB), "isPlayerControlled"), typeof(bool)));
        }

        public static List<CodeInstruction> InsertLogWithoutParameters(string log)
        {
            return new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "Logger")),
                    new CodeInstruction(OpCodes.Ldstr, log),
                    new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => Plugin.LogDebug(new string(new char[]{ })))),
                };
        }

        public static List<CodeInstruction> InsertIsBypass(List<CodeInstruction> codes,
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
                new CodeInstruction(OpCodes.Call, DisableOriginalGameDebugLogsMethod),
                new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
            };
            codes.InsertRange(startIndex, codesToAdd);
            return codes;
        }

        private static bool DisableOriginalGameDebugLogs()
        {
            return Const.DISABLE_ORIGINAL_GAME_DEBUG_LOGS;
        }

        private static bool AreInternsScheduledToLand()
        {
            return InternManager.Instance.AreInternsScheduledToLand();
        }
        private static bool IsPlayerLocalOrInternOwnerLocal(PlayerControllerB player)
        {
            return InternManager.Instance.IsPlayerLocalOrInternOwnerLocal(player);
        }
        private static int IndexBeginOfInterns()
        {
            return InternManager.Instance.IndexBeginOfInterns;
        }
        private static bool IsColliderFromLocalOrInternOwnerLocal(Collider collider)
        {
            return InternManager.Instance.IsColliderFromLocalOrInternOwnerLocal(collider);
        }
        private static bool IsPlayerIntern(PlayerControllerB player)
        {
            return InternManager.Instance.IsPlayerIntern(player);
        }
        private static bool IsIdPlayerIntern(int id)
        {
            return InternManager.Instance.IsIdPlayerIntern(id);
        }
        private static bool IsPlayerInternOwnerLocal(PlayerControllerB player)
        {
            return InternManager.Instance.IsPlayerInternOwnerLocal(player);
        }

        private static void UpdatePlayerAnimationServerRpc(ulong playerClientId, int animationState, float animationSpeed)
        {
            InternManager.Instance.GetInternAI((int)playerClientId)?.UpdateInternAnimationServerRpc(animationState,
                                                                                                    animationSpeed);
        }

        private static void SyncJump(ulong playerClientId)
        {
            InternManager.Instance.GetInternAI((int)playerClientId)?.SyncJump();
        }

        private static void SyncLandFromJump(ulong playerClientId, bool fallHard)
        {
            InternManager.Instance.GetInternAI((int)playerClientId)?.SyncLandFromJump(fallHard);
        }
    }
}
