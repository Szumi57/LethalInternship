using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    internal class StunGrenadeItemPatch
    {
        [HarmonyPatch("StunExplosion")]
        [HarmonyPostfix]
        static void StunExplosion_PostFix(Vector3 explosionPosition, 
                                          bool isHeldItem, 
                                          PlayerControllerB playerHeldBy)
        {
            // todo StunExplosion For later

            // for every intern { yada yada
            //PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
            //if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
            //{
            //    playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
            //}

            //if (isHeldItem && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
            //{
            //    GameNetworkManager.Instance.localPlayerController.DamagePlayer(20, false, true, CauseOfDeath.Blast, 0, false, default(Vector3));
            //}
        }
    }
}
