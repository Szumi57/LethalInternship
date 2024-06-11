using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.NpcPatches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        [HarmonyPatch("ChangeOwnershipOfEnemy")]
        [HarmonyPrefix]
        static bool ChangeOwnershipOfEnemy_PreFix(ref ulong newOwnerClientId)
        {
            Plugin.Logger.LogDebug($"Try ChangeOwnershipOfEnemy newOwnerClientId : {(int)newOwnerClientId}");
            InternAI? internAI = InternManager.Instance.GetInternAI((int)newOwnerClientId);
            if (internAI != null)
            {
                Plugin.Logger.LogDebug($"ChangeOwnershipOfEnemy not on intern but on intern owner : {internAI.OwnerClientId}");
                newOwnerClientId = internAI.OwnerClientId;
            }
            return true;
        }

        #region Transpilers

        [HarmonyPatch("MeetsStandardPlayerCollisionConditions")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MeetsStandardPlayerCollisionConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // bypass "component != GameNetworkManager.Instance.localPlayerController" if player is an intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].opcode == OpCodes.Brtrue
                    && codes[i + 1].opcode == OpCodes.Ldloc_0
                    && codes[i + 2].opcode == OpCodes.Call
                    && codes[i + 3].opcode == OpCodes.Ldfld
                    && codes[i + 4].opcode == OpCodes.Call
                    && codes[i + 8].opcode == OpCodes.Ldarg_0)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsColliderFromLocalOrInternOwnerLocalMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + 8].labels.First()/*IL_0051*/)
                };
                codes.InsertRange(startIndex + 1, codesToAdd);
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.EnemyAIPatch.MeetsStandardPlayerCollisionConditions_Transpiler could not insert instruction if is intern for \"component != GameNetworkManager.Instance.localPlayerController\".");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // bypass "component != GameNetworkManager.Instance.localPlayerController" if player is an intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"Update ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"Update ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldstr \"Set destination to target player A\""//227
                    && codes[i + 1].ToString() == "call static void UnityEngine.Debug::Log(object message)")
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
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.EnemyAIPatch.Update_Transpiler could not remove annoying log \"Set destination to target player A\"");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldstr \"Set destination to target player B\""//246
                    && codes[i + 1].ToString() == "call static void UnityEngine.Debug::Log(object message)")
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
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.EnemyAIPatch.Update_Transpiler could not remove annoying log \"Set destination to target player B\"");
            }

            return codes.AsEnumerable();
        }

        #endregion

        #region Post Fixes

        [HarmonyPatch("CheckLineOfSightForPlayer")]
        [HarmonyPostfix]
        static void CheckLineOfSightForPlayer_PostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, ref int range, int proximityAwareness)
        {
            PlayerControllerB internFound = null!;

            if (__instance.isOutside && !__instance.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            for (int i = InternManager.Instance.IndexBeginOfInterns; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB intern = StartOfRound.Instance.allPlayerScripts[i];
                if (!__instance.PlayerIsTargetable(intern))
                {
                    continue;
                }

                Vector3 position = intern.gameplayCamera.transform.position;
                if (Vector3.Distance(position, __instance.eye.position) < (float)range && !Physics.Linecast(__instance.eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = position - __instance.eye.position;
                    if (Vector3.Angle(__instance.eye.forward, to) < width || (proximityAwareness != -1 && Vector3.Distance(__instance.eye.position, position) < (float)proximityAwareness))
                    {
                        internFound = intern;
                    }
                }
            }

            if (__result == null && internFound == null)
            {
                return;
            }
            else if (__result == null && internFound != null)
            {
                Plugin.Logger.LogDebug("intern found, no player found");
                __result = internFound;
                return;
            }
            else if (__result != null && internFound == null)
            {
                Plugin.Logger.LogDebug("intern not found, player found");
                return;
            }
            else
            {
                if (__result == null || internFound == null) return;
                Vector3 playerPosition = __result.gameplayCamera.transform.position;
                Vector3 internPosition = internFound.gameplayCamera.transform.position;
                Vector3 aiPosition = __instance.eye == null ? __instance.transform.position : __instance.eye.position;
                if ((internPosition - aiPosition).sqrMagnitude < (playerPosition - aiPosition).sqrMagnitude)
                {
                    Plugin.Logger.LogDebug("intern closer");
                    __result = internFound;
                }
                else { Plugin.Logger.LogDebug("player closer"); }
            }
        }

        [HarmonyPatch("GetClosestPlayer")]
        [HarmonyPostfix]
        static void GetClosestPlayer_PostFix(EnemyAI __instance, ref PlayerControllerB __result, bool requireLineOfSight, bool cannotBeInShip, bool cannotBeNearShip)
        {
            PlayerControllerB internFound = null!;

            __instance.mostOptimalDistance = 2000f;
            for (int i = InternManager.Instance.IndexBeginOfInterns; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB intern = StartOfRound.Instance.allPlayerScripts[i];

                if (!__instance.PlayerIsTargetable(intern, cannotBeInShip, false))
                {
                    continue;
                }

                if (cannotBeNearShip)
                {
                    if (intern.isInElevator)
                    {
                        continue;
                    }
                    bool flag = false;
                    for (int j = 0; j < RoundManager.Instance.spawnDenialPoints.Length; j++)
                    {
                        if (Vector3.Distance(RoundManager.Instance.spawnDenialPoints[j].transform.position, intern.transform.position) < 10f)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        continue;
                    }
                }
                if (!requireLineOfSight || !Physics.Linecast(__instance.transform.position, intern.transform.position, 256))
                {
                    __instance.tempDist = Vector3.Distance(__instance.transform.position, intern.transform.position);
                    if (__instance.tempDist < __instance.mostOptimalDistance)
                    {
                        __instance.mostOptimalDistance = __instance.tempDist;
                        internFound = intern;
                    }
                }
            }

            if (__result == null && internFound == null)
            {
                return;
            }
            else if (__result == null && internFound != null)
            {
                __result = internFound;
                return;
            }
            else if (__result != null && internFound == null)
            {
                return;
            }
            else
            {
                if (__result == null || internFound == null) return;
                Vector3 playerPosition = __result.gameplayCamera.transform.position;
                Vector3 internPosition = internFound.gameplayCamera.transform.position;
                Vector3 aiPosition = __instance.eye == null ? __instance.transform.position : __instance.eye.position;
                if ((internPosition - aiPosition).sqrMagnitude < (playerPosition - aiPosition).sqrMagnitude)
                {
                    __result = internFound;
                }
            }
        }

        [HarmonyPatch("TargetClosestPlayer")]
        [HarmonyPostfix]
        static void TargetClosestPlayer_PostFix(EnemyAI __instance, ref bool __result, float bufferDistance, bool requireLineOfSight, float viewWidth)
        {
            __instance.mostOptimalDistance = 2000f;
            PlayerControllerB playerControllerB = __instance.targetPlayer;
            __instance.targetPlayer = null;
            for (int i = InternManager.Instance.IndexBeginOfInterns; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB intern = StartOfRound.Instance.allPlayerScripts[i];

                if (__instance.PlayerIsTargetable(intern, false, false)
                    && !__instance.PathIsIntersectedByLineOfSight(intern.transform.position, false, false)
                    && (!requireLineOfSight || __instance.CheckLineOfSightForPosition(intern.gameplayCamera.transform.position, viewWidth, 40, -1f, null)))
                {
                    __instance.tempDist = Vector3.Distance(__instance.transform.position, intern.transform.position);
                    if (__instance.tempDist < __instance.mostOptimalDistance)
                    {
                        __instance.mostOptimalDistance = __instance.tempDist;
                        __instance.targetPlayer = intern;
                    }
                }
            }
            if (__instance.targetPlayer != null && bufferDistance > 0f && playerControllerB != null
                && Mathf.Abs(__instance.mostOptimalDistance - Vector3.Distance(__instance.transform.position, playerControllerB.transform.position)) < bufferDistance)
            {
                __instance.targetPlayer = playerControllerB;
            }
            __result = __instance.targetPlayer != null;
        }

        #endregion
    }
}
