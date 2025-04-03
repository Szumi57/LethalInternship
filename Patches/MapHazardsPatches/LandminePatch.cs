using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Enums;
using LethalInternship.Interns;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for the <c>Landmine</c>
    /// </summary>
    [HarmonyPatch(typeof(Landmine))]
    public class LandminePatch
    {
        /// <summary>
        /// Patch for making the intern able to trigger the mine by stepping on it
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="other"></param>
        /// <param name="___localPlayerOnMine"></param>
        /// <param name="___pressMineDebounceTimer"></param>
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

            EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect != null
                && enemyAICollisionDetect.mainScript != null
                && enemyAICollisionDetect.mainScript.IsOwner
                && !enemyAICollisionDetect.mainScript.isEnemyDead)
            {
                InternAI? internAI = enemyAICollisionDetect.mainScript as InternAI;
                if (internAI != null
                    && internAI.IsOwner)
                {
                    ___localPlayerOnMine = true;
                    ___pressMineDebounceTimer = 0.5f;
                    __instance.PressMineServerRpc();

                    // Audio
                    internAI.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                    {
                        VoiceState = EnumVoicesState.SteppedOnTrap,
                        CanTalkIfOtherInternTalk = true,
                        WaitForCooldown = false,
                        CutCurrentVoiceStateToTalk = true,
                        CanRepeatVoiceState = false,

                        ShouldSync = false,
                        IsInternInside = internAI.NpcController.Npc.isInsideFactory,
                        AllowSwearing = Plugin.Config.AllowSwearing.Value
                    });
                }
            }
        }

        /// <summary>
        /// Patch for making the intern able to trigger the mine by stepping on it
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="other"></param>
        /// <param name="___localPlayerOnMine"></param>
        /// <param name="___mineActivated"></param>
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

            EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect != null
                && enemyAICollisionDetect.mainScript != null
                && enemyAICollisionDetect.mainScript.IsOwner
                && !enemyAICollisionDetect.mainScript.isEnemyDead)
            {
                InternAI? internAI = enemyAICollisionDetect.mainScript as InternAI;
                if (internAI != null
                    && internAI.IsOwner)
                {
                    ___localPlayerOnMine = false;

                    // Audio
                    internAI.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                    {
                        VoiceState = EnumVoicesState.SteppedOnTrap,
                        CanTalkIfOtherInternTalk = true,
                        WaitForCooldown = false,
                        CutCurrentVoiceStateToTalk = true,
                        CanRepeatVoiceState = true,

                        ShouldSync = false,
                        IsInternInside = internAI.NpcController.Npc.isInsideFactory,
                        AllowSwearing = Plugin.Config.AllowSwearing.Value
                    });

                    // Boom
                    TriggerMineOnLocalClientByExiting_ReversePatch(__instance);
                }
            }
        }

        /// <summary>
        /// Reverse patch for calling <c>TriggerMineOnLocalClientByExiting</c>z.
        /// Set the mine to explode.
        /// </summary>
        /// <param name="instance"></param>
        /// <exception cref="NotImplementedException">Ignore (see harmony)</exception>
        [HarmonyPatch("TriggerMineOnLocalClientByExiting")]
        [HarmonyReversePatch]
        public static void TriggerMineOnLocalClientByExiting_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.EnemiesPatches.TriggerMineOnLocalClientByExiting");

        /// <summary>
        /// Patch for making an explosion check for interns, calls for an explosion by landmine or lightning.
        /// </summary>
        /// <remarks>
        /// Strange behaviour where an entity is detect multiple times by <c>Physics.OverlapSphere</c>,<br/>
        /// so we need to check an entity only one time by using a list.
        /// </remarks>
        /// <param name="explosionPosition"></param>
        /// <param name="killRange"></param>
        /// <param name="damageRange"></param>
        /// <param name="nonLethalDamage"></param>
        [HarmonyPatch("SpawnExplosion")]
        [HarmonyPostfix]
        static void SpawnExplosion_PostFix(Vector3 explosionPosition, 
                                           float killRange, 
                                           float damageRange, 
                                           int nonLethalDamage)
        {
            Collider[] array = Physics.OverlapSphere(explosionPosition, damageRange, 8, QueryTriggerInteraction.Collide);
            PlayerControllerB internController;
            InternAI? internAI;
            List<ulong> internsAlreadyExploded = new List<ulong>();
            for (int i = 0; i < array.Length; i++)
            {
                Plugin.LogDebug($"SpawnExplosion OverlapSphere array {i} {array[i].name}");
                float distanceFromExplosion = Vector3.Distance(explosionPosition, array[i].transform.position);
                if (distanceFromExplosion > 4f
                    && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, out _, 256, QueryTriggerInteraction.Ignore))
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
                    Plugin.LogDebug($"SyncKillIntern from explosion for LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                    internController.KillPlayer(vector, spawnBody: true, CauseOfDeath.Blast, 0, default);
                }
                else if (distanceFromExplosion < damageRange)
                {
                    Vector3 vector = Vector3.Normalize(internController.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(internController.gameplayCamera.transform.position, explosionPosition);
                    internController.DamagePlayer(nonLethalDamage, hasDamageSFX: false, callRPC: false, CauseOfDeath.Blast, 0, false, vector * 0.6f);
                }

                internsAlreadyExploded.Add(internController.playerClientId);
            }
        }
    }
}
