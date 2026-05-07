using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(VehicleCollisionTrigger))]
    public class VehicleCollisionTriggerPatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        static void OnTriggerEnter_PostFix(Collider other)
        {
            IInternAI? internAI = null;

            if (other.gameObject.CompareTag("Player"))
            {
                PlayerControllerB componentPlayer = other.GetComponent<PlayerControllerB>();
                if (componentPlayer != null)
                {
                    internAI = InternManagerProvider.Instance.GetInternAI((int)componentPlayer.playerClientId);
                }
            }
            else if (other.gameObject.CompareTag("Enemy"))
            {
                EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (enemyAICollisionDetect != null
                    && enemyAICollisionDetect.mainScript != null
                    && enemyAICollisionDetect.mainScript.IsOwner
                    && !enemyAICollisionDetect.mainScript.isEnemyDead)
                {
                    internAI = enemyAICollisionDetect.mainScript as IInternAI;
                }
            }

            if (internAI != null)
            {
                internAI.OnCollisionWithCruiser();
            }
        }
    }
}
