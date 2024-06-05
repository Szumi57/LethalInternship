using HarmonyLib;
using LethalInternship.Managers;
using System;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropShipPatch
    {
        [HarmonyPatch("LandShipOnServer")]
        [HarmonyReversePatch]
        public static void LandShipOnServer_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.MapPatches.LandShipOnServer_ReversePatch");

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(ref ItemDropship __instance,
                                   StartOfRound ___playersManager,
                                   Terminal ___terminalScript)
        {
            if (!__instance.IsServer)
            {
                return;
            }

            if (InternManager.NbInternsToDropShip == 0)
            {
                return;
            }

            if (!__instance.deliveringOrder
                && ___terminalScript.orderedItemsFromTerminal.Count == 0)
            {
                if (___playersManager.shipHasLanded)
                {
                    __instance.shipTimer += Time.deltaTime;
                }
                if (__instance.playersFirstOrder)
                {
                    __instance.playersFirstOrder = false;
                    __instance.shipTimer = 20f;
                }
                // todo timer other mods ?
                if (__instance.shipTimer > 20f)
                {
                    LandShipOnServer_ReversePatch(__instance);
                    return;
                }
            }
        }

        [HarmonyPatch("OpenShipDoorsOnServer")]
        [HarmonyPostfix]
        static void OpenShipDoorsOnServer_PostFix(ref ItemDropship __instance)
        {
            if (InternManager.NbInternsToDropShip == 0)
            {
                return;
            }

            if (!__instance.shipLanded)
            {
                return;
            }

            InternManager.SpawnInternsFromDropShip(__instance.itemSpawnPositions);
        }
    }
}
