using HarmonyLib;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(BlobAI))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class BlobAIPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref Collider[] ___ragdollColliders)
        {
            Collider[] ragdollCollidersNewSize = new Collider[StartOfRound.Instance.allPlayerObjects.Length];
            ___ragdollColliders = ragdollCollidersNewSize;
        }
    }
}
