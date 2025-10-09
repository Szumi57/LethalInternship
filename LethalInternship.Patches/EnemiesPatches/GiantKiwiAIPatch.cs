using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(GiantKiwiAI))]
    public class GiantKiwiAIPatch
    {
        [HarmonyPatch("CheckLOSForCreatures")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CheckLOSForCreatures_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 10; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 522
                    && codes[i + 10].ToString().StartsWith("call void GiantKiwiAI::SyncWatchingThreatServerRpc")) // 532
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 11].labels.Add(labelToJumpTo); // 533

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(codes[startIndex]), // ldarg.0 NULL
                    new CodeInstruction(codes[startIndex+2]), // ldfld IVisibleThreat GiantKiwiAI::watchingThreat
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsThreatInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                //-----------------------------
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.GiantKiwiAIPatch.CheckLOSForCreatures_Transpiler could not bypass network object method call with intern");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("StartAttackingAndSync")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StartAttackingAndSync_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 14; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 134
                    && codes[i + 14].ToString().StartsWith("call void GiantKiwiAI::StartAttackingThreatServerRpc")) // 148
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 15].labels.Add(labelToJumpTo); // 149

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(codes[startIndex]), // ldarg.0 NULL
                    new CodeInstruction(codes[startIndex+2]), // ldfld IVisibleThreat GiantKiwiAI::watchingThreat
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsThreatInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                //-----------------------------
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.GiantKiwiAIPatch.StartAttackingAndSync_Transpiler could not could not bypass network object method call with intern");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        public static bool OnCollideWithPlayer_Prefix(GiantKiwiAI __instance,
                                                      Collider other,
                                                      ref float ___timeSinceHittingPlayer)
        {
            PlayerControllerB internController = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inKillAnimation, overrideIsInsideFactoryCheck: false);
            if (internController == null)
            {
                // Run base method
                return true;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)internController.playerClientId);
            if (internAI == null)
            {
                // Player
                return true;
            }

            // Intern
            if (internController.isInHangarShipRoom
                && !__instance.isInsidePlayerShip
                && internController.transform.position.y - __instance.transform.position.y > 1.6f
                && Physics.Linecast(__instance.transform.position + Vector3.up * 0.45f, internController.transform.position + Vector3.up * 0.45f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            ___timeSinceHittingPlayer = 0f;

            return false;
        }

        [HarmonyPatch("AnimationEventB")]
        [HarmonyPrefix]
        public static bool AnimationEventB_Prefix(GiantKiwiAI __instance,
                                                  IVisibleThreat ___attackingThreat,
                                                  bool ___attacking,
                                                  ref float ___timeSinceHittingGround,
                                                  ref int ___destroyTreesInterval,
                                                  ref float ___timeSinceHittingPlayer)
        {
            if (!___attacking || ___attackingThreat == null)
            {
                return true;
            }

            PlayerControllerB internController = ___attackingThreat.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (internController == null)
            {
                // Run base method
                return true;
            }
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)internController.playerClientId);
            if (internAI == null)
            {
                // Player
                return true;
            }

            // Intern
            __instance.AnimationEventA();
            __instance.peckAudio.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            RoundManager.Instance.PlayAudibleNoise(__instance.eye.position, 25f, 0.85f, 0, __instance.isInsidePlayerShip && StartOfRound.Instance.hangarDoorsClosed, 675188);
            if (___attacking && !__instance.isEnemyDead)
            {
                __instance.rocksParticle.Play();
                RoundManager.PlayRandomClip(__instance.peckAudio, __instance.attackSFX, true, 1f, 91911, 1000);
                ___timeSinceHittingGround = 0f;
                float num = Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, __instance.transform.position);
                if (num < 5f)
                {
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                }
                else if (num < 13f)
                {
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                }
                ___destroyTreesInterval = (___destroyTreesInterval + 1) % 4;
                if (___destroyTreesInterval == 0)
                {
                    RoundManager.Instance.DestroyTreeAtPosition(__instance.transform.position + __instance.transform.forward * 0.75f, 1.5f);
                }
                if (___timeSinceHittingPlayer < 0.1f)
                {
                    Vector3 a = internController.transform.position + Vector3.up * 3f - __instance.transform.position;
                    internController.externalForceAutoFade += a * __instance.hitVelocityForce;
                    internAI.SyncDamageIntern(10, CauseOfDeath.Stabbing, deathAnimation: 9, fallDamage: false, force: a * __instance.hitVelocityForce * 0.4f);
                    ___timeSinceHittingPlayer = 0f;
                }
                return false;
            }
            __instance.woodChipParticle.Play();
            __instance.peckAudio.PlayOneShot(__instance.peckTreeSFX);
            //WalkieTalkie.TransmitOneShotAudio(__instance.peckAudio, __instance.peckTreeSFX, 1f);

            return false;
        }
    }
}
