﻿using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace LethalInternship.Patches.MapHazardsPatches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        static void OnTriggerEnter_PostFix(ref Landmine __instance,
                                           Collider other,
                                           ref bool ___localPlayerOnMine,
                                           ref float ___pressMineDebounceTimer
                                           )
        {
            if (__instance.hasExploded)
            {
                return;
            }
            if (___pressMineDebounceTimer > 0f)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (InternManager.Instance.IsPlayerInternOwnerLocal(player))
                {
                    if (!player.isPlayerDead)
                    {
                        ___localPlayerOnMine = true;
                        ___pressMineDebounceTimer = 0.5f;
                        __instance.PressMineServerRpc();
                    }
                }
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        static void OnTriggerExit_PostFix(ref Landmine __instance,
                                          Collider other,
                                          ref bool ___localPlayerOnMine,
                                          bool ___mineActivated)
        {
            if (__instance.hasExploded)
            {
                return;
            }
            if (!___mineActivated)
            {
                return;
            }
            if (other.CompareTag("Player"))
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (InternManager.Instance.IsPlayerInternOwnerLocal(player))
                {
                    if (!player.isPlayerDead)
                    {
                        ___localPlayerOnMine = false;
                        TriggerMineOnLocalClientByExiting_ReversePatch(__instance);
                    }
                }
            }
        }

        [HarmonyPatch("TriggerMineOnLocalClientByExiting")]
        [HarmonyReversePatch]
        public static void TriggerMineOnLocalClientByExiting_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.EnemiesPatches.TriggerMineOnLocalClientByExiting");

        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPostfix]
        static void SpawnExplosion_PostFix(Vector3 explosionPosition, 
                                           float killRange, 
                                           float damageRange, 
                                           int nonLethalDamage)
        {
            Collider[] array = Physics.OverlapSphere(explosionPosition, damageRange, 8, QueryTriggerInteraction.Collide);
            RaycastHit raycastHit;
            PlayerControllerB internController;
            InternAI? internAI;
            List<ulong> internsAlreadyExploded = new List<ulong>();
            for (int i = 0; i < array.Length; i++)
            {
                Plugin.Logger.LogDebug($"array {i} {array[i].name}");
                float distanceFromExplosion = Vector3.Distance(explosionPosition, array[i].transform.position);
                if (distanceFromExplosion > 4f
                    && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, out raycastHit, 256, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (array[i].gameObject.layer != 3)
                {
                    continue;
                }

                internController = array[i].gameObject.GetComponent<PlayerControllerB>();
                if (internController == null)
                {
                    continue;
                }

                if (internsAlreadyExploded.Contains(internController.playerClientId))
                {
                    continue;
                }

                internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)internController.playerClientId);
                if (internAI == null)
                {
                    continue;
                }

                if (distanceFromExplosion < killRange)
                {
                    Vector3 vector = Vector3.Normalize(internController.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(internController.gameplayCamera.transform.position, explosionPosition);
                    Plugin.Logger.LogDebug($"SyncKillIntern from explosion for LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                    internAI.SyncKillIntern(vector, true, CauseOfDeath.Blast, 0);
                }
                else if (distanceFromExplosion < damageRange)
                {
                    Vector3 vector = Vector3.Normalize(internController.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(internController.gameplayCamera.transform.position, explosionPosition);
                    internAI.SyncDamageIntern(nonLethalDamage, CauseOfDeath.Blast, 0, false, vector * 0.6f);
                }

                internsAlreadyExploded.Add(internController.playerClientId);
            }
        }
    }
}
