using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
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
            if (__instance.mainScript.GetType() == typeof(IInternAI))
            {
                PlayerControllerB internController = other.gameObject.GetComponentInParent<PlayerControllerB>();
                if (internController != null
                    && InternManagerProvider.Instance.IsPlayerIntern(internController))
                {
                    Physics.IgnoreCollision(__instance.GetComponent<Collider>(), other);
                }
            }
        }
    }
}
