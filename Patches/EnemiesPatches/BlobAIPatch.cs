using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(BlobAI))]
    [HarmonyAfter(MoreCompany.PluginInformation.PLUGIN_GUID)]
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
