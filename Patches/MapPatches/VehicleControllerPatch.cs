using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    /// <summary>
    /// Patch for <c>VehicleController</c>
    /// </summary>
    [HarmonyPatch(typeof(VehicleController))]
    internal class VehicleControllerPatch
    {
        /// <summary>
        /// Patch for damaging the interns owned by client in vehicle
        /// </summary>
        [HarmonyPatch("DamagePlayerInVehicle")]
        [HarmonyPostfix]
        static void DamagePlayerInVehicle_PostFix(VehicleController __instance,
                                                  Vector3 vel, 
                                                  float magnitude)
        {
            PlayerControllerB internController;
            foreach(InternAI internAI in InternManager.Instance.GetInternsAIOwnedByLocal())
            {
                internController = internAI.NpcController.Npc;

                if (!__instance.localPlayerInPassengerSeat && !__instance.localPlayerInControl)
                {
                    if (__instance.physicsRegion.physicsTransform == internController.physicsParent
                        && internController.overridePhysicsParent == null)
                    {
                        internAI.SyncDamageIntern(10, CauseOfDeath.Inertia, 0, false, vel);
                        internController.externalForceAutoFade += vel;
                    }
                    return;
                }

                if (magnitude > 28f)
                {
                    internAI.SyncKillIntern(vel, true, CauseOfDeath.Inertia, 0, __instance.transform.up * 0.77f);
                    return;
                }

                if (magnitude <= 24f)
                {
                    internAI.SyncDamageIntern(30, CauseOfDeath.Inertia, 0, false, vel);
                    return;
                }

                if (internController.health < 20)
                {
                    internAI.SyncKillIntern(vel, true, CauseOfDeath.Inertia, 0, __instance.transform.up * 0.77f);
                    return;
                }
                internAI.SyncDamageIntern(40, CauseOfDeath.Inertia, 0, false, vel);
            }
        }

        /// <summary>
        /// Patch for killing intern when car is destroyed
        /// </summary>
        [HarmonyPatch("DestroyCar")]
        [HarmonyPostfix]
        static void DestroyCar_PostFix()
        {
            PlayerControllerB internController;
            foreach (InternAI internAI in InternManager.Instance.GetInternsAIOwnedByLocal())
            {
                internController = internAI.NpcController.Npc;

                Plugin.LogDebug($"DestroyCar Killing intern #{internAI.InternId}");
                internAI.SyncKillIntern(Vector3.up * 27f + 20f * Random.insideUnitSphere, true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f);
            }
        }
    }
}
