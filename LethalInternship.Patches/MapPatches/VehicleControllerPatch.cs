using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    /// <summary>
    /// Patch for <c>VehicleController</c>
    /// </summary>
    [HarmonyPatch(typeof(VehicleController))]
    public class VehicleControllerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix()
        {
            InternManagerProvider.Instance.VehicleHasLanded();
        }

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
            foreach (IInternAI internAI in InternManagerProvider.Instance.GetInternsAIOwnedByLocal())
            {
                if (internAI.NpcController.Npc.isPlayerDead)
                {
                    continue;
                }

                internController = internAI.NpcController.Npc;

                if (!__instance.localPlayerInPassengerSeat && !__instance.localPlayerInControl)
                {
                    if (__instance.physicsRegion.physicsTransform == internController.physicsParent
                        && internController.overridePhysicsParent == null)
                    {
                        internController.DamagePlayer(10, hasDamageSFX: false, callRPC: false, CauseOfDeath.Inertia, 0, false, vel);
                        internController.externalForceAutoFade += vel;
                    }
                    return;
                }

                if (magnitude > 28f)
                {
                    internController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, __instance.transform.up * 0.77f);
                    return;
                }

                if (magnitude <= 24f)
                {
                    internController.DamagePlayer(30, hasDamageSFX: false, callRPC: false, CauseOfDeath.Inertia, 0, false, vel);
                    return;
                }

                if (internController.health < 20)
                {
                    internController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, __instance.transform.up * 0.77f);
                    return;
                }
                internController.DamagePlayer(40, hasDamageSFX: false, callRPC: false, CauseOfDeath.Inertia, 0, false, vel);
            }
        }

        /// <summary>
        /// Patch for killing intern when car is destroyed
        /// </summary>
        [HarmonyPatch("DestroyCar")]
        [HarmonyPostfix]
        static void DestroyCar_PostFix()
        {
            foreach (IInternAI internAI in InternManagerProvider.Instance.GetInternsAIOwnedByLocal())
            {
                if (internAI.NpcController.Npc.isPlayerDead)
                {
                    continue;
                }

                PluginLoggerHook.LogDebug?.Invoke($"DestroyCar Killing intern #{internAI.NpcController.Npc.playerClientId}");
                internAI.NpcController.Npc.KillPlayer(Vector3.up * 27f + 20f * Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f);
            }
        }
    }
}
