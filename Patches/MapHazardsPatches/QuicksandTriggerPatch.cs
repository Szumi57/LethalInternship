using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Enums;
using LethalInternship.Interns;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for the <c>QuicksandTrigger</c>
    /// </summary>
    [HarmonyPatch(typeof(QuicksandTrigger))]
    public class QuicksandTriggerPatch
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
            InternAI? internAI = null;
            EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect != null
                && enemyAICollisionDetect.mainScript != null
                && enemyAICollisionDetect.mainScript.IsOwner
                && !enemyAICollisionDetect.mainScript.isEnemyDead)
            {
                internAI = enemyAICollisionDetect.mainScript as InternAI;
            }

            if (internAI == null)
            {
                return;
            }

            if (internAI.NpcController.IsControllerInCruiser)
            {
                return;
            }

            PlayerControllerB internController = internAI.NpcController.Npc;
            if (__instance.isWater && internController.underwaterCollider == null)
            {
                internController.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
            }
            internController.statusEffectAudioIndex = __instance.audioClipIndex;
            if (internController.isSinking)
            {
                if (!__instance.isWater)
                {
                    // Audio
                    internAI.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                    {
                        VoiceState = EnumVoicesState.Sinking,
                        CanTalkIfOtherInternTalk = true,
                        WaitForCooldown = false,
                        CutCurrentVoiceStateToTalk = true,
                        CanRepeatVoiceState = true,

                        ShouldSync = false,
                        IsInternInside = internAI.NpcController.Npc.isInsideFactory,
                        AllowSwearing = Plugin.Config.AllowSwearing.Value
                    });
                }
                return;
            }

            if (internAI.NpcController.CheckConditionsForSinkingInQuicksandIntern())
            {
                // Being sinking
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
                internAI.StopSinkingState();
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
            InternAI? internAI = null;
            EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect != null
                && enemyAICollisionDetect.mainScript != null
                && enemyAICollisionDetect.mainScript.IsOwner
                && !enemyAICollisionDetect.mainScript.isEnemyDead)
            {
                internAI = enemyAICollisionDetect.mainScript as InternAI;
            }

            if (internAI == null)
            {
                return;
            }

            if (internAI.NpcController.IsControllerInCruiser)
            {
                return;
            }

            internAI.StopSinkingState();
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
            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)playerScript.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            internAI.StopSinkingState();
            return false;
        }
    }
}
