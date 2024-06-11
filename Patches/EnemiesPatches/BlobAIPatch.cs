using HarmonyLib;
using LethalInternship.Managers;
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
            ___ragdollColliders = new Collider[InternManager.Instance.AllEntitiesCount];
        }
    }
}
