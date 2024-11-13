using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for the <c>QuicksandTrigger</c>
    /// </summary>
    [HarmonyPatch(typeof(QuicksandTrigger))]
    internal class QuicksandTriggerPatch
    {
        /// <summary>
        /// Patch for making quicksand works with intern, when entering
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="other"></param>
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        public static void OnTriggerStay_Postfix(ref QuicksandTrigger __instance, Collider other)
        {
            if (!__instance.isWater && !other.gameObject.CompareTag("Player"))
            {
                return;
            }

            PlayerControllerB internController = other.gameObject.GetComponent<PlayerControllerB>();
            if (internController == null)
            {
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)internController.playerClientId);
            if (internAI == null)
            {
                return;
            }

            if (__instance.isWater && internController.underwaterCollider == null)
            {
                internController.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
            }
            internController.statusEffectAudioIndex = __instance.audioClipIndex;
            if (internController.isSinking)
            {
                return;
            }

            if (internAI.NpcController.CheckConditionsForSinkingInQuicksandIntern())
            {
                internController.sourcesCausingSinking++;
                internController.isMovementHindered++;
                Plugin.LogDebug($"playerScript {internController.playerClientId} ++isMovementHindered {internController.isMovementHindered}");
                internController.hinderedMultiplier *= __instance.movementHinderance;
                if (__instance.isWater)
                {
                    internController.sinkingSpeedMultiplier = 0f;
                    return;
                }
                internController.sinkingSpeedMultiplier = __instance.sinkingSpeedMultiplier;
            }
            else
            {
                StopSinkingIntern(internController, __instance.movementHinderance);
            }
        }

        /// <summary>
        /// Patch for making quicksand works with intern, when exiting
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="other"></param>
        [HarmonyPatch("OnExit")]
        [HarmonyPostfix]
        public static void OnExit_Postfix(ref QuicksandTrigger __instance, Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerControllerB internController = other.gameObject.GetComponent<PlayerControllerB>();
            if (!InternManager.Instance.IsPlayerInternOwnerLocal(internController))
            {
                return;
            }

            StopSinkingIntern(internController, __instance.movementHinderance);
        }

        /// <summary>
        /// Patch for updating the right fields when an intern goes out of the quicksand
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="playerScript"></param>
        /// <returns></returns>
        [HarmonyPatch("StopSinkingLocalPlayer")]
        [HarmonyPrefix]
        public static bool StopSinkingLocalPlayer_Prefix(QuicksandTrigger __instance, PlayerControllerB playerScript)
        {
            if (!InternManager.Instance.IsPlayerInternOwnerLocal(playerScript))
            {
                return true;
            }

            StopSinkingIntern(playerScript, __instance.movementHinderance);
            return false;
        }

        private static void StopSinkingIntern(PlayerControllerB internController, float movementHinderance)
        {
            internController.sourcesCausingSinking = Mathf.Clamp(internController.sourcesCausingSinking - 100, 0, 100);
            internController.isMovementHindered = Mathf.Clamp(internController.isMovementHindered - 100, 0, 100);
            internController.hinderedMultiplier = Mathf.Clamp(internController.hinderedMultiplier / movementHinderance, 1f, 100f);
            internController.underwaterCollider = null;

            Plugin.LogDebug($"playerScript {internController.playerClientId} --playerScript.isMovementHindered {internController.isMovementHindered}");
        }
    }
}
