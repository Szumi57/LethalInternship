using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(PumaAI))]
    public class PumaAIPatch
    {
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIInterval_PostFix(PumaAI __instance)
        {
            int knowledgeIndex = 0;

            foreach (PlayerControllerB body in StartOfRound.Instance.allPlayerScripts)
            {
                if (body != null
                    && !body.isPlayerDead
                    && body.isPlayerControlled)
                {
                    // Agrandir uniquement si nécessaire
                    if (knowledgeIndex >= __instance.playerKnowledge.Length)
                    {
                        Array.Resize(ref __instance.playerKnowledge, knowledgeIndex + 1);
                    }

                    __instance.playerKnowledge[knowledgeIndex] = new PumaPlayerKnowledge(body);
                    knowledgeIndex++;
                }
            }
        }

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPostfix]
        static void OnCollideWithPlayer_PostFix(PumaAI __instance, Collider other)
        {
            PlayerControllerB? body = other.gameObject.GetComponent<PlayerControllerB>();
            if (__instance.targetPlayer == body
                && InternManagerProvider.Instance.IsPlayerIntern(body))
            {
                __instance.SwitchToBehaviourState(2);
            }
        }
    }
}
