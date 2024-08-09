using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.NpcPatches
{
    /// <summary>
    /// Patch for the internAI
    /// </summary>
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        /// <summary>
        /// Patch for intercepting when ownership of an enemy changes.<br/>
        /// Only change ownership to a irl player, if new owner is intern then new owner is the owner (real player) of the intern
        /// </summary>
        /// <param name="newOwnerClientId"></param>
        /// <returns></returns>
        [HarmonyPatch("ChangeOwnershipOfEnemy")]
        [HarmonyPrefix]
        static bool ChangeOwnershipOfEnemy_PreFix(ref ulong newOwnerClientId)
        {
            Plugin.LogDebug($"Try ChangeOwnershipOfEnemy newOwnerClientId : {(int)newOwnerClientId}");
            InternAI? internAI = InternManager.Instance.GetInternAI((int)newOwnerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"ChangeOwnershipOfEnemy not on intern but on intern owner : {internAI.OwnerClientId}");
                newOwnerClientId = internAI.OwnerClientId;
            }
            return true;
        }

        #region Transpilers

        /// <summary>
        /// Patch for making the enemy able to detect an intern when colliding
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
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
                Plugin.LogError($"LethalInternship.Patches.NpcPatches.EnemyAIPatch.MeetsStandardPlayerCollisionConditions_Transpiler could not insert instruction if is intern for \"component != GameNetworkManager.Instance.localPlayerController\".");
            }

            return codes.AsEnumerable();
        }

        #endregion

        #region Post Fixes

        /// <summary>
        /// Patch for making the enemy check intern too when calling <c>CheckLineOfSightForPlayer</c>
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="width"></param>
        /// <param name="range"></param>
        /// <param name="proximityAwareness"></param>
        [HarmonyPatch("CheckLineOfSightForPlayer")]
        [HarmonyPostfix]
        static void CheckLineOfSightForPlayer_PostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, ref int range, int proximityAwareness)
        {
            PlayerControllerB internControllerFound = null!;

            if (__instance.isOutside && !__instance.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            for (int i = InternManager.Instance.IndexBeginOfInterns; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB internController = StartOfRound.Instance.allPlayerScripts[i];
                if (!__instance.PlayerIsTargetable(internController))
                {
                    continue;
                }

                Vector3 position = internController.gameplayCamera.transform.position;
                if (Vector3.Distance(position, __instance.eye.position) < (float)range && !Physics.Linecast(__instance.eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = position - __instance.eye.position;
                    if (Vector3.Angle(__instance.eye.forward, to) < width || (proximityAwareness != -1 && Vector3.Distance(__instance.eye.position, position) < (float)proximityAwareness))
                    {
                        internControllerFound = internController;
                    }
                }
            }

            if (__result == null && internControllerFound == null)
            {
                return;
            }
            else if (__result == null && internControllerFound != null)
            {
                Plugin.LogDebug("intern found, no player found");
                __result = internControllerFound;
                return;
            }
            else if (__result != null && internControllerFound == null)
            {
                Plugin.LogDebug("intern not found, player found");
                return;
            }
            else
            {
                if (__result == null || internControllerFound == null) return;
                Vector3 playerPosition = __result.gameplayCamera.transform.position;
                Vector3 internPosition = internControllerFound.gameplayCamera.transform.position;
                Vector3 aiEnemyPosition = __instance.eye == null ? __instance.transform.position : __instance.eye.position;
                if ((internPosition - aiEnemyPosition).sqrMagnitude < (playerPosition - aiEnemyPosition).sqrMagnitude)
                {
                    Plugin.LogDebug("intern closer");
                    __result = internControllerFound;
                }
                else { Plugin.LogDebug("player closer"); }
            }
        }

        /// <summary>
        /// Patch for making the enemy check intern too when calling <c>GetClosestPlayer</c>
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="requireLineOfSight"></param>
        /// <param name="cannotBeInShip"></param>
        /// <param name="cannotBeNearShip"></param>
        [HarmonyPatch("GetClosestPlayer")]
        [HarmonyPostfix]
        static void GetClosestPlayer_PostFix(EnemyAI __instance, ref PlayerControllerB __result, bool requireLineOfSight, bool cannotBeInShip, bool cannotBeNearShip)
        {
            PlayerControllerB internControllerFound = null!;

            for (int i = InternManager.Instance.IndexBeginOfInterns; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB internController = StartOfRound.Instance.allPlayerScripts[i];

                if (!__instance.PlayerIsTargetable(internController, cannotBeInShip, false))
                {
                    continue;
                }

                if (cannotBeNearShip)
                {
                    if (internController.isInElevator)
                    {
                        continue;
                    }
                    bool flag = false;
                    for (int j = 0; j < RoundManager.Instance.spawnDenialPoints.Length; j++)
                    {
                        if (Vector3.Distance(RoundManager.Instance.spawnDenialPoints[j].transform.position, internController.transform.position) < 10f)
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
                if (!requireLineOfSight || !Physics.Linecast(__instance.transform.position, internController.transform.position, 256))
                {
                    __instance.tempDist = Vector3.Distance(__instance.transform.position, internController.transform.position);
                    if (__instance.tempDist < __instance.mostOptimalDistance)
                    {
                        __instance.mostOptimalDistance = __instance.tempDist;
                        internControllerFound = internController;
                    }
                }
            }

            if (__result == null && internControllerFound == null)
            {
                return;
            }
            else if (__result == null && internControllerFound != null)
            {
                __result = internControllerFound;
                return;
            }
            else if (__result != null && internControllerFound == null)
            {
                return;
            }
            else
            {
                if (__result == null || internControllerFound == null) return;
                Vector3 playerPosition = __result.gameplayCamera.transform.position;
                Vector3 internPosition = internControllerFound.gameplayCamera.transform.position;
                Vector3 aiEnemyPosition = __instance.eye == null ? __instance.transform.position : __instance.eye.position;
                if ((internPosition - aiEnemyPosition).sqrMagnitude < (playerPosition - aiEnemyPosition).sqrMagnitude)
                {
                    __result = internControllerFound;
                }
            }
        }

        /// <summary>
        /// Patch for making the enemy check intern too when calling <c>TargetClosestPlayer</c>
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="bufferDistance"></param>
        /// <param name="requireLineOfSight"></param>
        /// <param name="viewWidth"></param>
        [HarmonyPatch("TargetClosestPlayer")]
        [HarmonyPostfix]
        static void TargetClosestPlayer_PostFix(EnemyAI __instance, ref bool __result, float bufferDistance, bool requireLineOfSight, float viewWidth)
        {
            PlayerControllerB playerTargetted = __instance.targetPlayer;
            
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
            if (__instance.targetPlayer != null && bufferDistance > 0f && playerTargetted != null
                && Mathf.Abs(__instance.mostOptimalDistance - Vector3.Distance(__instance.transform.position, playerTargetted.transform.position)) < bufferDistance)
            {
                __instance.targetPlayer = playerTargetted;
            }
            __result = __instance.targetPlayer != null;
        }

        #endregion
    }
}
