using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Patches.Utils
{
    public static class PatchesUtil
    {
        public static readonly FieldInfo FieldInfoWasUnderwaterLastFrame = AccessTools.Field(typeof(PlayerControllerB), "wasUnderwaterLastFrame");
        public static readonly FieldInfo FieldInfoPlayerClientId = AccessTools.Field(typeof(PlayerControllerB), "playerClientId");
        public static readonly FieldInfo FieldInfoPreviousAnimationStateHash = AccessTools.Field(typeof(PlayerControllerB), "previousAnimationStateHash");
        public static readonly FieldInfo FieldInfoCurrentAnimationStateHash = AccessTools.Field(typeof(PlayerControllerB), "currentAnimationStateHash");
        public static readonly FieldInfo FieldInfoTargetPlayer = AccessTools.Field(typeof(BushWolfEnemy), "targetPlayer");
        public static readonly FieldInfo FieldInfoDraggingPlayer = AccessTools.Field(typeof(BushWolfEnemy), "draggingPlayer");

        public static readonly MethodInfo AllEntitiesCountMethod = SymbolExtensions.GetMethodInfo(() => AllEntitiesCount());
        public static readonly MethodInfo AreInternsScheduledToLandMethod = SymbolExtensions.GetMethodInfo(() => AreInternsScheduledToLand());
        public static readonly MethodInfo IsPlayerLocalOrInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerLocalOrInternOwnerLocal(new PlayerControllerB()));
        public static readonly MethodInfo IsColliderFromLocalOrInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsColliderFromLocalOrInternOwnerLocal(new Collider()));
        public static readonly MethodInfo IndexBeginOfInternsMethod = SymbolExtensions.GetMethodInfo(() => IndexBeginOfInterns());
        public static readonly MethodInfo IsPlayerInternMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerIntern(new PlayerControllerB()));
        public static readonly MethodInfo IsIdPlayerInternMethod = SymbolExtensions.GetMethodInfo(() => IsIdPlayerIntern(new int()));
        public static readonly MethodInfo IsRagdollPlayerIdInternMethod = SymbolExtensions.GetMethodInfo(() => IsRagdollPlayerIdIntern(new RagdollGrabbableObject()));
        public static readonly MethodInfo IsPlayerInternOwnerLocalMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerInternOwnerLocal(new PlayerControllerB()));
        public static readonly MethodInfo IsAnInternAiOwnerOfObjectMethod = SymbolExtensions.GetMethodInfo(() => IsAnInternAiOwnerOfObject((GrabbableObject)new object()));
        public static readonly MethodInfo DisableOriginalGameDebugLogsMethod = SymbolExtensions.GetMethodInfo(() => DisableOriginalGameDebugLogs());
        public static readonly MethodInfo IsPlayerInternControlledAndOwnerMethod = SymbolExtensions.GetMethodInfo(() => IsPlayerInternControlledAndOwner(new PlayerControllerB()));
        public static readonly MethodInfo GetDamageFromSlimeIfInternMethod = SymbolExtensions.GetMethodInfo(() => GetDamageFromSlimeIfIntern(new PlayerControllerB()));
        public static readonly MethodInfo SyncWatchingThreatIfInternMethod = SymbolExtensions.GetMethodInfo(() => SyncWatchingThreatIfIntern(new GiantKiwiAI(), new PlayerControllerB()));
        public static readonly MethodInfo SyncAttackingThreatIfInternMethod = SymbolExtensions.GetMethodInfo(() => SyncAttackingThreatIfIntern(new GiantKiwiAI(), new PlayerControllerB()));
        public static readonly MethodInfo SyncSetTargetToThreatIfInternMethod = SymbolExtensions.GetMethodInfo(() => SyncSetTargetToThreatIfIntern(new RadMechAI(), new PlayerControllerB(), new Vector3()));
        public static readonly MethodInfo ShouldShovelIgnoreInternMethod = SymbolExtensions.GetMethodInfo(() => ShouldShovelIgnoreIntern(new Shovel()));

        public static readonly MethodInfo GetGameobjectMethod = AccessTools.PropertyGetter(typeof(UnityEngine.Component), "gameObject");

        public static readonly MethodInfo SyncJumpMethod = SymbolExtensions.GetMethodInfo(() => SyncJump(new ulong()));
        public static readonly MethodInfo SyncLandFromJumpMethod = SymbolExtensions.GetMethodInfo(() => SyncLandFromJump(new ulong(), new bool()));


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

        //public static List<CodeInstruction> InsertLogOfFieldOfThis(string logWithZeroParameter, FieldInfo fieldInfo, Type fieldType)
        //{
        //    return new List<CodeInstruction>()
        //        {
        //            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "Logger")),
        //            new CodeInstruction(OpCodes.Ldstr, logWithZeroParameter),
        //            new CodeInstruction(OpCodes.Ldarg_0),
        //            new CodeInstruction(OpCodes.Ldfld, fieldInfo),
        //            new CodeInstruction(OpCodes.Box, fieldType),
        //            new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => String.Format(new string(new char[]{ }), new object()))),
        //            new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => PluginLoggerHook.LogDebug?.Invoke(new string(new char[]{ })))),
        //        };

        //    //codes.InsertRange(0, PatchesUtil.InsertLogOfFieldOfThis("isPlayerControlled {0}", AccessTools.Field(typeof(PlayerControllerB), "isPlayerControlled"), typeof(bool)));
        //}

        //public static List<CodeInstruction> InsertLogWithoutParameters(string log)
        //{
        //    return new List<CodeInstruction>()
        //        {
        //            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Plugin), "Logger")),
        //            new CodeInstruction(OpCodes.Ldstr, log),
        //            new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => PluginLoggerHook.LogDebug?.Invoke(new string(new char[]{ })))),
        //        };
        //}

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

        public static bool IsGrabbableObjectEqualsToNull(GrabbableObject grabbableObject)
        {
            return grabbableObject == null;
        }

        private static bool DisableOriginalGameDebugLogs()
        {
            return Const.DISABLE_ORIGINAL_GAME_DEBUG_LOGS;
        }

        private static bool AreInternsScheduledToLand()
        {
            return InternManagerProvider.Instance.AreInternsScheduledToLand();
        }
        private static bool IsPlayerLocalOrInternOwnerLocal(PlayerControllerB player)
        {
            return InternManagerProvider.Instance.IsPlayerLocalOrInternOwnerLocal(player);
        }
        private static int IndexBeginOfInterns()
        {
            return InternManagerProvider.Instance.IndexBeginOfInterns;
        }
        private static int AllEntitiesCount()
        {
            return InternManagerProvider.Instance.AllEntitiesCount;
        }
        private static bool IsColliderFromLocalOrInternOwnerLocal(Collider collider)
        {
            return InternManagerProvider.Instance.IsColliderFromLocalOrInternOwnerLocal(collider);
        }
        private static bool IsPlayerIntern(PlayerControllerB player)
        {
            return InternManagerProvider.Instance.IsPlayerIntern(player);
        }
        private static bool IsIdPlayerIntern(int id)
        {
            return InternManagerProvider.Instance.IsIdPlayerIntern(id);
        }
        private static bool IsRagdollPlayerIdIntern(RagdollGrabbableObject ragdollGrabbableObject)
        {
            return InternManagerProvider.Instance.IsIdPlayerIntern((int)ragdollGrabbableObject.ragdoll.playerScript.playerClientId);
        }
        private static bool IsPlayerInternOwnerLocal(PlayerControllerB player)
        {
            return InternManagerProvider.Instance.IsPlayerInternOwnerLocal(player);
        }
        private static bool IsPlayerInternControlledAndOwner(PlayerControllerB player)
        {
            return InternManagerProvider.Instance.IsPlayerInternControlledAndOwner(player);
        }
        private static int GetDamageFromSlimeIfIntern(PlayerControllerB player)
        {
            return InternManagerProvider.Instance.GetDamageFromSlimeIfIntern(player);
        }
        private static bool SyncWatchingThreatIfIntern(GiantKiwiAI giantKiwiAI, IVisibleThreat threat)
        {
            PlayerControllerB internController = threat.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (internController == null)
            {
                return false;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)internController.playerClientId);
            if (internAI == null)
            {
                // Player
                return false;
            }

            internAI.SyncWatchingThreatGiantKiwiServerRpc(giantKiwiAI.GetComponent<NetworkObject>());

            return true;
        }
        private static bool SyncAttackingThreatIfIntern(GiantKiwiAI giantKiwiAI, IVisibleThreat threat)
        {
            PlayerControllerB internController = threat.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (internController == null)
            {
                return false;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)internController.playerClientId);
            if (internAI == null)
            {
                // Player
                return false;
            }

            internAI.SyncAttackingThreatGiantKiwiServerRpc(giantKiwiAI.GetComponent<NetworkObject>());

            return true;
        }
        private static bool SyncSetTargetToThreatIfIntern(RadMechAI radMechAI, IVisibleThreat threat, Vector3 lastSeenPos)
        {
            PlayerControllerB internController = threat.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (internController == null)
            {
                return false;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)internController.playerClientId);
            if (internAI == null)
            {
                // Player
                return false;
            }

            internAI.SyncSetTargetToThreatServerRpc(radMechAI.GetComponent<NetworkObject>(), lastSeenPos);

            return true;
        }

        private static bool IsAnInternAiOwnerOfObject(GrabbableObject grabbableObject)
        {
            return InternManagerProvider.Instance.IsAnInternAiOwnerOfObject(grabbableObject);
        }

        private static void SyncJump(ulong playerClientId)
        {
            InternManagerProvider.Instance.GetInternAI((int)playerClientId)?.SyncJump();
        }

        private static void SyncLandFromJump(ulong playerClientId, bool fallHard)
        {
            InternManagerProvider.Instance.GetInternAI((int)playerClientId)?.SyncLandFromJump(fallHard);
        }

        private static bool ShouldShovelIgnoreIntern(Shovel shovel)
        {
            // Is an intern attacking ?
            return InternManagerProvider.Instance.GetInternAI((int)shovel.playerHeldBy.playerClientId) != null;
        }
    }
}
