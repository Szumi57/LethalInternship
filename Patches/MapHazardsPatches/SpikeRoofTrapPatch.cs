using HarmonyLib;
using LethalInternship.AI;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for <c>SpikeRoofTrap</c>
    /// </summary>
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    internal class SpikeRoofTrapPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPostfix]
        static void OnTriggerStay_PostFix(Collider other)
        {
            EnemyAICollisionDetect enemyAICollisionDetect = other.gameObject.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect != null 
                && enemyAICollisionDetect.mainScript != null 
                && enemyAICollisionDetect.mainScript.IsOwner 
                && enemyAICollisionDetect.mainScript.enemyType.canDie 
                && !enemyAICollisionDetect.mainScript.isEnemyDead)
            {
                InternAI? internAI = enemyAICollisionDetect.mainScript as InternAI;
                if (internAI != null)
                {
                    internAI.SyncKillIntern(Vector3.down * 17f, true, CauseOfDeath.Crushing, 0, default(Vector3));
                }
            }
        }
    }
}
