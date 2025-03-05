using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.NpcPatches
{
    [HarmonyPatch(typeof(EnemyAICollisionDetect))]
    public class EnemyAICollisionDetectPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        static void Prefix(EnemyAICollisionDetect __instance, Collider other)
        {
            if (__instance.mainScript.GetType() == typeof(InternAI))
            {
                PlayerControllerB internController = other.gameObject.GetComponentInParent<PlayerControllerB>();
                if (internController != null
                    && InternManager.Instance.IsPlayerIntern(internController))
                {
                    Physics.IgnoreCollision(__instance.GetComponent<Collider>(), other);
                }
            }
        }
    }
}
