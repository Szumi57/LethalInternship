using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for the <c>BlobAI</c>
    /// </summary>
    [HarmonyPatch(typeof(BlobAI))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class BlobAIPatch
    {
        /// <summary>
        /// Patch the numbers of ragdoll colliders of the <c>BlobAI</c>
        /// </summary>
        /// <param name="___ragdollColliders"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref Collider[] ___ragdollColliders)
        {
            ___ragdollColliders = new Collider[InternManager.Instance.AllEntitiesCount];
        }
    }
}
