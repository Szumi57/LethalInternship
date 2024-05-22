using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropShipPatch
    {
        private static Traverse? _traverseMethodLandShipOnServer;
        private static bool _isInternSpawned;

        #region ItemDropShip private methods

        private static void LandShipOnServer()
        {
            _traverseMethodLandShipOnServer?.GetValue();
        }

        #endregion

        //[HarmonyPatch("Update")]
        //[HarmonyPostfix]
        static void Update_PostFix(ref ItemDropship __instance,
                                                StartOfRound ___playersManager,
                                                ref int ___timesPlayedWithoutTurningOff,
                                                ref float ___noiseInterval)
        {
            if (!__instance.IsServer)
            {
                return;
            }

            if(_isInternSpawned) { return; }

            if (_traverseMethodLandShipOnServer == null)
            {
                _traverseMethodLandShipOnServer = Traverse.Create(__instance).Method("LandShipOnServer");
                if (_traverseMethodLandShipOnServer != null && !_traverseMethodLandShipOnServer.MethodExists())
                {
                    Plugin.Logger.LogError($"Method ItemDropship.LandShipOnServer not found");
                }
            }

            if (!__instance.deliveringOrder)
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
                if (__instance.shipTimer > 20f)
                {
                    LandShipOnServer();
                    return;
                }
            }
            else if (__instance.shipLanded)
            {
                __instance.shipTimer += Time.deltaTime;
                if (__instance.shipTimer > 30f)
                {
                    ___timesPlayedWithoutTurningOff = 0;
                    __instance.shipLanded = false;
                    __instance.ShipLeaveClientRpc();
                }
                if (___noiseInterval <= 0f)
                {
                    ___noiseInterval = 1f;
                    ___timesPlayedWithoutTurningOff++;
                    RoundManager.Instance.PlayAudibleNoise(__instance.transform.position, 60f, 1.3f, ___timesPlayedWithoutTurningOff, false, 94);
                    return;
                }
                ___noiseInterval -= Time.deltaTime;
            }
        }

        [HarmonyPatch("OpenShipDoorsOnServer")]
        [HarmonyPostfix]
        static void OpenShipDoorsOnServer_PostFix(ref ItemDropship __instance,
                                                               bool ___shipLanded,
                                                               Transform[] ___itemSpawnPositions)
        {
            if (!___shipLanded)
            {
                return;
            }

            StartOfRoundPatch.SpawnIntern(___itemSpawnPositions[0], true);
            _isInternSpawned = true;

            __instance.shipLanded = false;
            __instance.ShipLeaveClientRpc();
        }
    }
}
