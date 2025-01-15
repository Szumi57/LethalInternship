using FasterItemDropship;
using HarmonyLib;
using LethalInternship.Constants;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.FasterItemDropship
{
    [HarmonyPatch(typeof(ItemDropship))]
    public class FasterItemDropshipPatch
    {
        private static float previousShipTimer = 0f;
        private static bool previousFirstOrder = true;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        [HarmonyBefore(Const.FASTERITEMDROPSHIP_GUID)]
        static void Update_PreFix(ItemDropship __instance,
                                  Terminal ___terminalScript)
        {
            if (!__instance.IsServer)
            {
                return;
            }

            previousShipTimer = __instance.shipTimer;
            previousFirstOrder = __instance.playersFirstOrder;

            if (!__instance.deliveringOrder
                && !StartOfRound.Instance.shipHasLanded
                && ConfigSettings.startDeliveryBeforePlayerShipLanded.Value)
            {
                if (___terminalScript.orderedItemsFromTerminal.Count > 0 || ___terminalScript.orderedVehicleFromTerminal != -1 || InternManager.Instance.AreInternsScheduledToLand())
                {
                    __instance.shipTimer += Time.deltaTime;
                }

                if (___terminalScript.orderedItemsFromTerminal.Count > 0 || ___terminalScript.orderedVehicleFromTerminal != -1)
                {
                    // Cancel the update
                    __instance.shipTimer -= Time.deltaTime;
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        [HarmonyAfter(Const.FASTERITEMDROPSHIP_GUID)]
        static void Update_Postfix(ItemDropship __instance,
                                   Terminal ___terminalScript)
        {
            if (__instance.IsServer
                && !__instance.deliveringOrder
                && (__instance.shipTimer < previousShipTimer || previousFirstOrder && !__instance.playersFirstOrder)
                && (___terminalScript.orderedItemsFromTerminal.Count > 0 || ___terminalScript.orderedVehicleFromTerminal != -1 || InternManager.Instance.AreInternsScheduledToLand()))
            {
                __instance.shipTimer = 40 - ConfigSettings.dropshipDeliveryTime.Value;
            }
        }
    }
}
