using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    public class StunGrenadeItemPatch
    {
        [HarmonyPatch("SetControlTipForGrenade")]
        [HarmonyPrefix]
        static bool SetControlTipForGrenade_PreFix(StunGrenadeItem __instance)
        {
            if (InternManager.Instance.IsAnInternAiOwnerOfObject((GrabbableObject)__instance))
            {
                return false;
            }

            return true;
        }

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
