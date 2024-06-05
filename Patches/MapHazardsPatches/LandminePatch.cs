using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Utils;
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
                if (PatchesUtil.IsPlayerInternOwnerLocal(player))
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
                if (PatchesUtil.IsPlayerInternOwnerLocal(player))
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

    }
}
