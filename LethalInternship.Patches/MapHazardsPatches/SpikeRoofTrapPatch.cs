using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for <c>SpikeRoofTrap</c>
    /// </summary>
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    public class SpikeRoofTrapPatch
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
                IInternAI? internAI = enemyAICollisionDetect.mainScript as IInternAI;
                if (internAI != null)
                {
                    internAI.NpcController.Npc.KillPlayer(Vector3.down * 17f, spawnBody: true, CauseOfDeath.Crushing, 0, default(Vector3));
                }
            }
        }
    }
}
