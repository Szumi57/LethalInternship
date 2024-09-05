using HarmonyLib;
using LethalInternship.Managers;
using System;
using UnityEngine;
using Random = System.Random;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporterPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyAfter(Const.MORECOMPANY_GUID)]
        [HarmonyPostfix]
        public static void Awake_Postfix(ref ShipTeleporter __instance, ref int[] ___playersBeingTeleported)
        {
            int[] array = new int[InternManager.Instance.AllEntitiesCount];
            Array.Fill(array, -1);
            ___playersBeingTeleported = array;
        }

        [HarmonyPatch("TeleportPlayerOutWithInverseTeleporter")]
        [HarmonyPostfix]
        static void TeleportPlayerOutWithInverseTeleporter_PostFix(ShipTeleporter __instance, 
                                                                   int playerObj,
                                                                   Vector3 teleportPos,
                                                                   Random ___shipTeleporterSeed)
        {
            InternManager.Instance.TeleportOutInterns(__instance, playerObj, teleportPos, ___shipTeleporterSeed);
        }
    }
}
