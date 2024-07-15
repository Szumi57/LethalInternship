using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for <c>BushWolfEnemy</c>
    /// </summary>
    [HarmonyPatch(typeof(BushWolfEnemy))]
    internal class BushWolfEnemyPatch
    {
        /// <summary>
        /// Patch for making the bush wolf be able to kill an intern
        /// </summary>
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPostfix]
        static void OnCollideWithPlayer_PostFix(BushWolfEnemy __instance,
                                                Collider other,
                                                bool ___foundSpawningPoint,
                                                bool ___inKillAnimation,
                                                Vector3 ___currentHidingSpot,
                                                float ___timeSinceTakingDamage,
                                                PlayerControllerB ___lastHitByPlayer,
                                                bool ___dragging,
                                                bool ___startedShootingTongue)
        {
            if (!___foundSpawningPoint)
            {
                return;
            }
            if (___inKillAnimation)
            {
                return;
            }
            if (__instance.isEnemyDead)
            {
                return;
            }

            PlayerControllerB playerController = __instance.MeetsStandardPlayerCollisionConditions(other, ___inKillAnimation, false);
            if (playerController == null)
            {
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI == null)
            {
                return;
            }

            Plugin.LogDebug($"fox saw intern #{internAI.InternId}");
            float num = Vector3.Distance(__instance.transform.position, ___currentHidingSpot);
            bool flag = false;
            if (___timeSinceTakingDamage < 2.5f && ___lastHitByPlayer != null && num < 16f)
            {
                flag = true;
            }
            else if (num < 7f && ___dragging && !___startedShootingTongue && __instance.targetPlayer == playerController)
            {
                flag = true;
            }
            if (flag)
            {
                internAI.SyncKillIntern(Vector3.up * 15f, true, CauseOfDeath.Mauling, 8, default);
                __instance.DoKillPlayerAnimationServerRpc((int)__instance.targetPlayer.playerClientId);
            }
        }
    }
}
