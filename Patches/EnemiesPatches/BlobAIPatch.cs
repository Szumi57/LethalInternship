using HarmonyLib;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(BlobAI))]
    [HarmonyAfter("me.swipez.melonloader.morecompany")]
    internal class BlobAIPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref BlobAI __instance)
        {
            Collider[] nearPlayerCollidersNewSize = new Collider[StartOfRound.Instance.allPlayerObjects.Length];
            Traverse.Create(__instance).Field("nearPlayerColliders").SetValue(nearPlayerCollidersNewSize);
        }
    }
}
