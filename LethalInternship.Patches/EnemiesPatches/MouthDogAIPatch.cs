﻿using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for the <c>MouthDogAI</c>
    /// </summary>
    [HarmonyPatch(typeof(MouthDogAI))]
    public class MouthDogAIPatch
    {
        /// <summary>
        /// Patch to make mouthdog ignoring InternAI (does not ignore Intern body <c>PlayerController</c>)
        /// </summary>
        /// <param name="other"></param>
        /// <param name="collidedEnemy"></param>
        /// <returns></returns>
        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        static bool OnCollideWithEnemy_PreFix(Collider other, EnemyAI collidedEnemy)
        {
            if (collidedEnemy == null)
            {
                return true;
            }

            if (collidedEnemy is IInternAI)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch to make the mouthDog kill the intern before running base game code
        /// </summary>
        /// <param name="playerId">Id player (maybe intern) that the mouth dog tries to kill</param>
        /// <returns></returns>
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayer_PreFix(int playerId)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAIIfLocalIsOwner(playerId);
            if (internAI == null)
            {
                return true;
            }

            PluginLoggerHook.LogDebug?.Invoke($"SyncKillIntern from mouthdogAI Intern #{internAI.NpcController.Npc.playerClientId}");
            internAI.NpcController.Npc.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Mauling, 0, default);

            return true;
        }

        /// <summary>
        /// <inheritdoc cref="ButlerBeesEnemyAIPatch.OnCollideWithPlayer_Transpiler"/>
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="other"></param>
        /// <param name="___inKillAnimation"></param>
        /// <returns></returns>
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool OnCollideWithPlayer_PreFix(ref MouthDogAI __instance,
                                               Collider other,
                                               bool ___inKillAnimation)
        {
            PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, ___inKillAnimation, false);
            if (playerControllerB == null)
            {
                return false;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAIIfLocalIsOwner((int)playerControllerB.playerClientId);
            if (internAI == null)
            {
                // Not intern or intern not owned by local
                return true;
            }

            Vector3 a = Vector3.Normalize((__instance.transform.position + Vector3.up - playerControllerB.gameplayCamera.transform.position) * 100f);
            if (!Physics.Linecast(__instance.transform.position + Vector3.up + a * 0.5f, playerControllerB.gameplayCamera.transform.position, out RaycastHit raycastHit, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (__instance.currentBehaviourStateIndex == 0 || __instance.currentBehaviourStateIndex == 1)
                {
                    __instance.SwitchToBehaviourState(2);
                    __instance.ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
                    __instance.SetDestinationToPosition(playerControllerB.transform.position, false);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Patch update to manipulate field for not breaking lunge system after targeting or kill an intern
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="___inKillAnimation"></param>
        /// <param name="___inLunge"></param>
        /// <param name="___lungeCooldown"></param>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(ref MouthDogAI __instance,
                                   bool ___inKillAnimation,
                                   bool ___inLunge,
                                   ref float ___lungeCooldown)
        {
            if (__instance.currentBehaviourStateIndex == 2
                && __instance.endingLunge
                && ___inLunge
                && !___inKillAnimation)
            {
                ___lungeCooldown = 0.25f;
                __instance.EndLungeServerRpc();
            }
        }
    }
}