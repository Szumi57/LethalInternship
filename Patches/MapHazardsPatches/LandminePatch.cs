using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using System;
using UnityEngine;

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
            Collider[] array = Physics.OverlapSphere(explosionPosition, damageRange, 2621448, QueryTriggerInteraction.Collide);
            RaycastHit raycastHit;
            PlayerControllerB player;
            for (int i = 0; i < array.Length; i++)
            {
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

                player = array[i].gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }

                if (!InternManager.Instance.IsPlayerInternOwnerLocal(player))
                {
                    continue;
                }

                if (distanceFromExplosion < killRange)
                {
                    Vector3 vector = Vector3.Normalize(player.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(player.gameplayCamera.transform.position, explosionPosition);
                    player.KillPlayer(vector, true, CauseOfDeath.Blast, 0);
                }
                else if (distanceFromExplosion < damageRange)
                {
                    Vector3 vector = Vector3.Normalize(player.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(player.gameplayCamera.transform.position, explosionPosition);
                    player.DamagePlayer(nonLethalDamage, true, true, CauseOfDeath.Blast, 0, false, vector * 0.6f);
                }
            }
        }
    }
}
