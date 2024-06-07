using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(MouthDogAI))]
    internal class MouthDogAIPatch
    {
        [HarmonyPatch("OnCollideWithEnemy")]
        [HarmonyPrefix]
        static bool OnCollideWithEnemy_PreFix(Collider other, EnemyAI collidedEnemy)
        {
            if (collidedEnemy == null)
            {
                return true;
            }

            if (InternManager.Instance.IsAIInternAi(collidedEnemy))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayer_PreFix(int playerId)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI(playerId);
            if (internAI == null)
            {
                return true;
            }

            if (internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
            {
                internAI.NpcController.Npc.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 0);
            }

            return true;
        }

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        static bool OnCollideWithPlayer_PreFix(ref MouthDogAI __instance,
                                               Collider other,
                                               bool ___inKillAnimation)
        {
            PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, ___inKillAnimation, false);
            if (playerControllerB == null)
            {
                return false;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerControllerB.playerClientId);
            if (internAI == null)
            {
                // Not intern
                return true;
            }

            Vector3 a = Vector3.Normalize((__instance.transform.position + Vector3.up - playerControllerB.gameplayCamera.transform.position) * 100f);
            if (!Physics.Linecast(__instance.transform.position + Vector3.up + a * 0.5f, playerControllerB.gameplayCamera.transform.position, out RaycastHit raycastHit, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                if (__instance.currentBehaviourStateIndex == 0 || __instance.currentBehaviourStateIndex == 1)
                {
                    __instance.SwitchToBehaviourState(2);
                    __instance.ChangeOwnershipOfEnemy(playerControllerB.playerClientId);
                    __instance.SetDestinationToPosition(playerControllerB.transform.position, false);
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(ref MouthDogAI __instance,
                                   bool ___inKillAnimation,
                                   bool ___inLunge,
                                   ref float ___lungeCooldown)
        {
            if (__instance.currentBehaviourStateIndex == 2
                && __instance.endingLunge
                && ___inLunge
                && !___inKillAnimation)
            {
                ___lungeCooldown = 0.25f;
                __instance.EndLungeServerRpc();
            }
        }
    }
}