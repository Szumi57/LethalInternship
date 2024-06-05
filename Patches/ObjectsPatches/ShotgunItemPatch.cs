using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(ShotgunItem))]
    internal class ShotgunItemPatch
    {
        [HarmonyPatch("ShootGun")]
        [HarmonyPostfix]
        static void ShootGun_PostFix(ShotgunItem __instance,
                                     Vector3 shotgunPosition,
                                     Vector3 shotgunForward)
        {
            ulong localPlayerActualId = GameNetworkManager.Instance.localPlayerController.actualClientId;

            for (int i = InternManager.Instance.IndexBeginToInterns; i < InternManager.Instance.AllEntitiesCount; i++)
            {
                PlayerControllerB intern = StartOfRound.Instance.allPlayerScripts[i];
                if (intern.isPlayerDead || !intern.isPlayerControlled)
                {
                    continue;
                }

                if (intern.OwnerClientId != localPlayerActualId)
                {
                    continue;
                }

                int damage = 0;
                float distanceTarget = Vector3.Distance(intern.transform.position, __instance.shotgunRayPoint.transform.position);
                Vector3 contactPointTarget = intern.playerCollider.ClosestPoint(shotgunPosition);

                if (Vector3.Angle(shotgunForward, contactPointTarget - shotgunPosition) < 30f
                    && !Physics.Linecast(shotgunPosition, contactPointTarget, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    if (distanceTarget < 5f)
                    {
                        damage = 100;
                    }
                    if (distanceTarget < 15f)
                    {
                        damage = 100;
                    }
                    else if (distanceTarget < 23f)
                    {
                        damage = 40;
                    }
                    else if (distanceTarget < 30f)
                    {
                        damage = 20;
                    }

                    Plugin.Logger.LogDebug($"Dealing {damage} damage to intern {intern.name} {intern.playerClientId}, owner {intern.OwnerClientId}");
                    intern.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, __instance.shotgunRayPoint.forward * 30f);
                }
            }
        }
    }
}
