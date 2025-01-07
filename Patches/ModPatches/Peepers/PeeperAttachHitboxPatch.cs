using GameNetcodeStuff;
using HarmonyLib;
using LCPeeper;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.Peepers
{
    [HarmonyPatch(typeof(PeeperAttachHitbox))]
    internal class PeeperAttachHitboxPatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        public static void OnTriggerEnter_Postfix(PeeperAttachHitbox __instance, Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerControllerB playerControllerB = other.gameObject.GetComponent<PlayerControllerB>();
                if (playerControllerB != null
                    && InternManager.Instance.IsPlayerIntern(playerControllerB))
                {
                    __instance.mainScript.AttachToPlayerServerRpc(playerControllerB.playerClientId);
                }
            }
            else if (other is BoxCollider)
            {
                // intern character controller is inactive but box collider (spine, thighs, arms) still procs
                PlayerControllerB playerControllerB = other.gameObject.GetComponentInParent<PlayerControllerB>();
                if (playerControllerB != null
                    && InternManager.Instance.IsPlayerIntern(playerControllerB))
                {
                    __instance.mainScript.AttachToPlayerServerRpc(playerControllerB.playerClientId);
                }
            }
        }
    }
}
