using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    [HarmonyPatch(typeof(QuicksandTrigger))]
    internal class QuicksandTriggerPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStay_Postfix(ref QuicksandTrigger __instance, Collider other)
        {
            if (!__instance.isWater && !other.gameObject.CompareTag("Player"))
            {
                return;
            }

            PlayerControllerB intern = other.gameObject.GetComponent<PlayerControllerB>();
            if (!InternManager.Instance.IsPlayerInternOwnerLocal(intern))
            {
                return;
            }

            if (__instance.isWater && !intern.isUnderwater)
            {
                intern.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                intern.isUnderwater = true;
            }
            intern.statusEffectAudioIndex = __instance.audioClipIndex;
            if (intern.isSinking)
            {
                return;
            }

            if (intern.CheckConditionsForSinkingInQuicksand())
            {
                intern.sourcesCausingSinking++;
                intern.isMovementHindered++;
                intern.hinderedMultiplier *= __instance.movementHinderance;
                if (__instance.isWater)
                {
                    intern.sinkingSpeedMultiplier = 0f;
                    return;
                }
                intern.sinkingSpeedMultiplier = __instance.sinkingSpeedMultiplier;
            }
            else
            {
                __instance.StopSinkingLocalPlayer(intern);
            }
        }

        [HarmonyPatch("OnExit")]
        [HarmonyPostfix]
        public static void OnExit_Postfix(ref QuicksandTrigger __instance, Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerControllerB intern = other.gameObject.GetComponent<PlayerControllerB>();
            if (!InternManager.Instance.IsPlayerInternOwnerLocal(intern))
            {
                return;
            }
            __instance.StopSinkingLocalPlayer(intern);
        }

        [HarmonyPatch("StopSinkingLocalPlayer")]
        [HarmonyPrefix]
        public static bool StopSinkingLocalPlayer_Prefix(QuicksandTrigger __instance, PlayerControllerB playerScript)
        {
            if (!InternManager.Instance.IsPlayerInternOwnerLocal(playerScript))
            {
                return true;
            }

            playerScript.sourcesCausingSinking = Mathf.Clamp(playerScript.sourcesCausingSinking - 1, 0, 100);
            playerScript.isMovementHindered = Mathf.Clamp(playerScript.isMovementHindered - 1, 0, 100);
            playerScript.hinderedMultiplier = Mathf.Clamp(playerScript.hinderedMultiplier / __instance.movementHinderance, 1f, 100f);
            if (playerScript.isMovementHindered == 0 && __instance.isWater)
            {
                playerScript.isUnderwater = false;
            }

            return false;
        }
    }
}
