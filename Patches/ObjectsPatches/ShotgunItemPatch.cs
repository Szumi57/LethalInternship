using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Interns.AI;
using LethalInternship.Managers;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.Patches.ObjectsPatches
{
    /// <summary>
    /// Patches for <c>ShotgunItem</c>
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem))]
    public class ShotgunItemPatch
    {
        /// <summary>
        /// Patch to make the shotgun able to damage/kill intern, held by players or enemies
        /// </summary>
        [HarmonyPatch("ShootGun")]
        [HarmonyPostfix]
        static void ShootGun_PostFix(ShotgunItem __instance,
                                     Vector3 shotgunPosition,
                                     Vector3 shotgunForward)
        {
            PlayerControllerB internController;
            InternAI? internAI;
            for (int i = InternManager.Instance.IndexBeginOfInterns; i < InternManager.Instance.AllEntitiesCount; i++)
            {
                internController = StartOfRound.Instance.allPlayerScripts[i];
                if (internController.isPlayerDead || !internController.isPlayerControlled)
                {
                    continue;
                }

                internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)internController.playerClientId);
                if (internAI == null)
                {
                    continue;
                }

                int damage = 0;
                Vector3 internPos = internController.transform.position + new Vector3(0, 1f, 0);
                float distanceTarget = Vector3.Distance(internPos, __instance.shotgunRayPoint.transform.position);
                Vector3 contactPointTarget = internPos;
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

                    internController.DamagePlayer(damage, hasDamageSFX: false, callRPC: false, CauseOfDeath.Gunshots, 0, false, __instance.shotgunRayPoint.forward * 30f);
                }
            }
        }
    }
}
