using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LethalInternship.Patches.NpcPatches
{
    /// <summary>
    /// Patch for <c>PlayerControllerB</c>
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        #region Prefixes

        /// <summary>
        /// Patch for intercepting the update and using only the intern update for intern.<br/>
        /// Need to pass back and forth the private fields before and after modifying them.
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyAfter(Const.MOREEMOTES_GUID)]
        [HarmonyPrefix]
        static bool Update_PreFix(PlayerControllerB __instance,
                                  ref bool ___isCameraDisabled,
                                  bool ___isJumping,
                                  bool ___isFallingFromJump,
                                  ref float ___crouchMeter,
                                  ref bool ___isWalking,
                                  ref float ___playerSlidingTimer,
                                  ref bool ___disabledJetpackControlsThisFrame,
                                  ref bool ___startedJetpackControls,
                                  ref float ___timeSinceTakingGravityDamage,
                                  ref bool ___teleportingThisFrame,
                                  ref float ___previousFrameDeltaTime,
                                  ref float ___cameraUp,
                                  ref float ___updatePlayerLookInterval)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI == null)
            {
                if ((int)__instance.playerClientId >= InternManagerProvider.Instance.IndexBeginOfInterns)
                {
                    return false;
                }

                return true;
            }

            if (internAI.NpcController == null)
            {
                // Cancel update for intern
                return false;
            }

            // Use Intern update and pass all needed paramaters back and forth
            internAI.NpcController.IsCameraDisabled = ___isCameraDisabled;
            internAI.NpcController.IsJumping = ___isJumping;
            internAI.NpcController.IsFallingFromJump = ___isFallingFromJump;
            internAI.NpcController.CrouchMeter = ___crouchMeter;
            internAI.NpcController.IsWalking = ___isWalking;
            internAI.NpcController.PlayerSlidingTimer = ___playerSlidingTimer;

            internAI.NpcController.DisabledJetpackControlsThisFrame = ___disabledJetpackControlsThisFrame;
            internAI.NpcController.StartedJetpackControls = ___startedJetpackControls;
            internAI.NpcController.TimeSinceTakingGravityDamage = ___timeSinceTakingGravityDamage;
            internAI.NpcController.TeleportingThisFrame = ___teleportingThisFrame;
            internAI.NpcController.PreviousFrameDeltaTime = ___previousFrameDeltaTime;

            internAI.NpcController.CameraUp = ___cameraUp;
            internAI.NpcController.UpdatePlayerLookInterval = ___updatePlayerLookInterval;

            internAI.UpdateController();

            ___isCameraDisabled = internAI.NpcController.IsCameraDisabled;
            ___crouchMeter = internAI.NpcController.CrouchMeter;
            ___isWalking = internAI.NpcController.IsWalking;
            ___playerSlidingTimer = internAI.NpcController.PlayerSlidingTimer;

            ___startedJetpackControls = internAI.NpcController.StartedJetpackControls;
            ___timeSinceTakingGravityDamage = internAI.NpcController.TimeSinceTakingGravityDamage;
            ___teleportingThisFrame = internAI.NpcController.TeleportingThisFrame;
            ___previousFrameDeltaTime = internAI.NpcController.PreviousFrameDeltaTime;

            ___cameraUp = internAI.NpcController.CameraUp;
            ___updatePlayerLookInterval = internAI.NpcController.UpdatePlayerLookInterval;

            return false;
        }

        /// <summary>
        /// Patch for intercepting the LateUpdate and using only the intern LateUpdate for intern.<br/>
        /// Need to pass back and forth the private fields before and after modifying them.
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_PreFix(PlayerControllerB __instance,
                                      ref bool ___isWalking,
                                      ref bool ___updatePositionForNewlyJoinedClient,
                                      ref float ___updatePlayerLookInterval,
                                      int ___playerMask)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                if (internAI.NpcController == null)
                {
                    // Cancel late update for intern
                    return false;
                }

                internAI.NpcController.IsWalking = ___isWalking;
                internAI.NpcController.UpdatePositionForNewlyJoinedClient = ___updatePositionForNewlyJoinedClient;
                internAI.NpcController.UpdatePlayerLookInterval = ___updatePlayerLookInterval;
                internAI.NpcController.PlayerMask = ___playerMask;

                internAI.NpcController.LateUpdate();

                ___isWalking = internAI.NpcController.IsWalking;
                ___updatePositionForNewlyJoinedClient = internAI.NpcController.UpdatePositionForNewlyJoinedClient;
                ___updatePlayerLookInterval = internAI.NpcController.UpdatePlayerLookInterval;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch to disabling the base game awake method for the intern
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool Awake_PreFix(PlayerControllerB __instance)
        {
            // Spawning network managers
            var nm = NetworkManager.Singleton;
            if (!InternManagerProvider.IsReady)
            {
                if (nm.IsHost || nm.IsServer)
                {
                    // Server
                    PluginLoggerHook.LogInfo?.Invoke("Spawning InternManager networkBehaviour in PlayerControllerBPatch Awake_PreFix");
                    var go = Object.Instantiate(PluginManagerProvider.Instance.InternManagerPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            if (!SaveManagerProvider.IsReady)
            {
                if (nm.IsHost || nm.IsServer)
                {
                    // Server
                    PluginLoggerHook.LogInfo?.Invoke("Spawning SaveManager networkBehaviour in PlayerControllerBPatch Awake_PreFix");
                    var go = Object.Instantiate(PluginManagerProvider.Instance.SaveManagerPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            if (!TerminalManagerProvider.IsReady)
            {
                if (nm.IsHost || nm.IsServer)
                {
                    // Server
                    PluginLoggerHook.LogInfo?.Invoke("Spawning TerminalManager networkBehaviour in PlayerControllerBPatch Awake_PreFix");
                    var go = Object.Instantiate(PluginManagerProvider.Instance.TerminalManagerPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            // -------------------

            if (!InternManagerProvider.IsReady)
            {
                return true;
            }

            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                // ignore awake if intern
                return false;
            }

            // If player
            if (__instance.gameObject.GetComponent<IgnoreRaycast>() == null)
            {
                __instance.gameObject.AddComponent<IgnoreRaycast>();
            }

            // continue awake if player
            return true;
        }

        /// <summary>
        /// Patch for calling the right method to damage intern
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        static bool DamagePlayer_PreFix(PlayerControllerB __instance,
                                        int damageNumber,
                                        CauseOfDeath causeOfDeath,
                                        int deathAnimation,
                                        bool fallDamage,
                                        Vector3 force)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"SyncDamageIntern called from game code on LOCAL client, intern object: Intern #{internAI.NpcController.Npc.playerClientId}");
                internAI.SyncDamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);

                // Still do the vanilla damage player, for other mods prefixes (ex: peepers)
                // The damage will be ignored because the intern playerController is not owned because not spawned
                return true;
            }

            if (DebugConst.NO_DAMAGE)
            {
                // Bootleg invulnerability
                PluginLoggerHook.LogDebug?.Invoke($"Bootleg invulnerability (return false)");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Patch to call the right method to damage intern from other player
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("DamagePlayerFromOtherClientServerRpc")]
        [HarmonyPrefix]
        static bool DamagePlayerFromOtherClientServerRpc_PreFix(PlayerControllerB __instance,
                                                                int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"SyncDamageInternFromOtherClient called from game code on LOCAL client, intern object: Intern #{internAI.NpcController.Npc.playerClientId}");
                internAI.DamageInternFromOtherClientServerRpc(damageAmount, hitDirection, playerWhoHit);

                // Send vanilla damage player, for other mods prefixes (ex: peepers)
                // The damage function will be ignored because the intern playerController is not owned because not spawned
                internAI.NpcController.Npc.DamagePlayer(damageAmount, hasDamageSFX: false, callRPC: false, CauseOfDeath.Bludgeoning, deathAnimation: 0, fallDamage: false, default(Vector3));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Damage to call the right method to kill intern
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayer_PreFix(PlayerControllerB __instance,
                                      Vector3 bodyVelocity,
                                      bool spawnBody,
                                      CauseOfDeath causeOfDeath,
                                      int deathAnimation,
                                      Vector3 positionOffset)
        {
            // Try to kill an intern ?
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"SyncKillIntern called from game code on LOCAL client, intern object: Intern #{internAI.NpcController.Npc.playerClientId}");
                internAI.SyncKillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);

                // Send vanilla kill player, for other mods prefixes (ex: peepers)
                // The kill function will be ignored because the intern playerController is not owned because not spawned
                return true;
            }

            // A player is killed 
            if (DebugConst.NO_DEATH)
            {
                // Bootleg invincibility
                PluginLoggerHook.LogDebug?.Invoke($"Bootleg invincibility");
                return false;
            }

            // Check if we hold interns
            IInternAI[] internsAIsHoldByPlayer = InternManagerProvider.Instance.GetInternsAiHoldByPlayer((int)__instance.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                IInternAI internAIHeld;
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internAIHeld = internsAIsHoldByPlayer[i];
                    switch (causeOfDeath)
                    {
                        case CauseOfDeath.Gravity:
                        case CauseOfDeath.Blast:
                        case CauseOfDeath.Suffocation:
                        case CauseOfDeath.Drowning:
                        case CauseOfDeath.Abandoned:
                            PluginLoggerHook.LogDebug?.Invoke($"SyncKillIntern on held intern by {__instance.playerUsername} {__instance.playerClientId} on LOCAL client, Intern #{internAIHeld.NpcController.Npc.playerClientId}");
                            internAIHeld.SyncKillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
                            break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Patch to call the right method for update special animation value for the intern
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("UpdateSpecialAnimationValue")]
        [HarmonyPrefix]
        static bool UpdateSpecialAnimationValue_PreFix(PlayerControllerB __instance,
                                                       bool specialAnimation, float timed, bool climbingLadder)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                internAI.UpdateInternSpecialAnimationValue(specialAnimation, timed, climbingLadder);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch for player to be able to take item from intern if pointing at item held in hands,<br/>
        /// makes the intern drop and immediately grab by the player
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("BeginGrabObject")]
        [HarmonyPrefix]
        static bool BeginGrabObject_PreFix(PlayerControllerB __instance,
                                           ref Ray ___interactRay,
                                           ref RaycastHit ___hit,
                                           ref int ___interactableObjectsMask)
        {
            ___interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (Physics.Raycast(___interactRay, out ___hit, __instance.grabDistance, ___interactableObjectsMask)
                && ___hit.collider.gameObject.layer != 8
                && ___hit.collider.tag == "PhysicsProp")
            {
                GrabbableObject grabbableObject = ___hit.collider.transform.gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    // Quit and continue original method
                    return true;
                }

                if (!grabbableObject.isHeld)
                {
                    // Quit and continue original method
                    return true;
                }

                IInternAI? internAI = InternManagerProvider.Instance.GetInternAiOwnerOfObject(grabbableObject);
                if (internAI == null)
                {
                    // Quit and continue original method
                    PluginLoggerHook.LogDebug?.Invoke($"BeginGrabObject_PreFix: no intern found who hold item {grabbableObject.name}");
                    return true;
                }

                PluginLoggerHook.LogDebug?.Invoke($"intern {internAI.NpcController.Npc.playerUsername} drop item {grabbableObject.name} before grab by player");
                grabbableObject.isHeld = false;
                internAI.DropItem(grabbableObject);
            }

            return true;
        }

        /// <summary>
        /// Patch to call the right the right method for sync dead body if the intern is calling it
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("SyncBodyPositionClientRpc")]
        [HarmonyPrefix]
        static bool SyncBodyPositionClientRpc_PreFix(PlayerControllerB __instance, Vector3 newBodyPosition)
        {
            // send to server if intern from controller
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"NetworkManager {__instance.NetworkManager}, newBodyPosition {newBodyPosition}, this.deadBody {__instance.deadBody}");
                internAI.SyncDeadBodyPositionServerRpc(newBodyPosition);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch for calling intern method if intern
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyPrefix]
        static bool PlayerHitGroundEffects_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                PlayerHitGroundEffects_ReversePatch(__instance);
                return false;
            }

            return true;
        }

        [HarmonyPatch("IncreaseFearLevelOverTime")]
        [HarmonyPrefix]
        static bool IncreaseFearLevelOverTime_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch("JumpToFearLevel")]
        [HarmonyPrefix]
        static bool JumpToFearLevel_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch("PerformEmote")]
        [HarmonyPrefix]
        static bool PerformEmote_PreFix(PlayerControllerB __instance, int emoteID)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (!CheckConditionsForEmote_ReversePatch(__instance))
            {
                return false;
            }

            __instance.performingEmote = true;
            __instance.playerBodyAnimator.SetInteger("emoteNumber", emoteID);
            internAI.StartPerformingEmoteInternServerRpc(emoteID);

            return false;
        }

        /// <summary>
        /// Prefix for using the intern server rpc for emotes, for the ownership false
        /// </summary>
        /// <remarks>Calls from MoreEmotes mod typically</remarks>
        /// <returns></returns>
        [HarmonyPatch("StartPerformingEmoteServerRpc")]
        [HarmonyPrefix]
        static bool StartPerformingEmoteServerRpc_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            internAI.StartPerformingEmoteInternServerRpc(__instance.playerBodyAnimator.GetInteger("emoteNumber"));
            return false;
        }

        [HarmonyPatch("ConnectClientToPlayerObject")]
        [HarmonyPrefix]
        static bool ConnectClientToPlayerObject_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch("TeleportPlayer")]
        [HarmonyPrefix]
        static bool TeleportPlayer_PreFix(PlayerControllerB __instance,
                                          Vector3 pos)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                internAI.TeleportIntern(pos);
                return false;
            }

            return true;
        }

        [HarmonyPatch("PlayFootstepServer")]
        [HarmonyPrefix]
        static bool PlayFootstepServer_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                internAI.NpcController.PlayFootstep(isServer: true);
                return false;
            }

            return true;
        }

        [HarmonyPatch("PlayFootstepLocal")]
        [HarmonyPrefix]
        static bool PlayFootstepLocal_PreFix(PlayerControllerB __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                internAI.NpcController.PlayFootstep(isServer: false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// See <see cref="StopHoldInteractionOnTrigger_PostFix"><c>StopHoldInteractionOnTrigger_PostFix</c></see>
        /// </summary>
        /// <param name="__state"></param>
        /// <returns></returns>
        //[HarmonyPatch("StopHoldInteractionOnTrigger")]
        //[HarmonyPrefix]
        //static bool StopHoldInteractionOnTrigger_PreFix(out float __state)
        //{
        //    __state = InputManager.Instance.OpenCommandsInternInputIsPressed ? HUDManager.Instance.holdFillAmount : 0;
        //    // see postfix
        //    return true;
        //}

        #endregion

        #region Reverse patches

        /// <summary>
        /// Reverse patch to call <c>PlayJumpAudio</c>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("PlayJumpAudio")]
        [HarmonyReversePatch]
        public static void PlayJumpAudio_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayJumpAudio_ReversePatch");

        /// <summary>
        /// Reverse patch modified to use the right method to sync land from jump for the intern
        /// </summary>
        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyReversePatch]
        public static void PlayerHitGroundEffects_ReversePatch(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 9; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 17
                        && codes[i + 9].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::LandFromJumpRpc(")) // 26
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    // Remove hitSoft parameter loading
                    codes[startIndex + 5].opcode = OpCodes.Nop; // 22 ldarg.0 NULL
                    codes[startIndex + 5].operand = null;
                    codes[startIndex + 6].opcode = OpCodes.Nop; // 23 ldfld float GameNetcodeStuff.PlayerControllerB::fallValue
                    codes[startIndex + 6].operand = null;
                    codes[startIndex + 7].opcode = OpCodes.Nop; // 24 ldc.r4 -1
                    codes[startIndex + 7].operand = null;
                    codes[startIndex + 8].opcode = OpCodes.Nop; // 25 clt NULL
                    codes[startIndex + 8].operand = null;

                    // Replace method
                    // Reverse patch only called for interns
                    codes[startIndex + 9].operand = PatchesUtil.SyncLandFromJumpMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch could not use jump from land method for intern");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <summary>
        /// Reverse patch to call <c>CheckConditionsForEmote</c>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("CheckConditionsForEmote")]
        [HarmonyReversePatch]
        public static bool CheckConditionsForEmote_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchCheckConditionsForEmote_ReversePatch");

        /// <summary>
        /// Reverse patch to call <c>OnDisable</c>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("OnDisable")]
        [HarmonyReversePatch]
        public static void OnDisable_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.OnDisable_ReversePatch");

        [HarmonyPatch("InteractTriggerUseConditionsMet")]
        [HarmonyReversePatch]
        public static bool InteractTriggerUseConditionsMet_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.InteractTriggerUseConditionsMet_ReversePatch");

        /// <summary>
        /// Reverse patch to be able to call <c>IsInSpecialAnimationClientRpc</c>
        /// </summary>
        /// <remarks>
        /// Bypassing all rpc condition, because the intern is not owner of his body, no one is, the body <c>PlayerControllerB</c> of intern is not spawned.<br/>
        /// </remarks>
        [HarmonyPatch("IsInSpecialAnimationClientRpc")]
        [HarmonyReversePatch]
        public static void IsInSpecialAnimationClientRpc_ReversePatch(object instance, bool specialAnimation, float timed, bool climbingLadder)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 3; i++)
                {
                    if (codes[i].ToString().StartsWith("call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()")// 70
                        && codes[i + 3].ToString().StartsWith("ldstr \"Setting animation on client\""))// 73
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes.Insert(0, new CodeInstruction(OpCodes.Br, codes[startIndex + 3].labels[0]));
                    startIndex = -1;
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch could not bypass rpc stuff");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <summary>
        /// Reverse patch to be able to call <c>SyncBodyPositionClientRpc</c>
        /// </summary>
        /// <remarks>
        /// Bypassing all rpc condition, because the intern is not owner of his body, no one is, the body <c>PlayerControllerB</c> of intern is not spawned.<br/>
        /// </remarks>
        [HarmonyPatch("SyncBodyPositionClientRpc")]
        [HarmonyReversePatch]
        public static void SyncBodyPositionClientRpc_ReversePatch(object instance, Vector3 newBodyPosition)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 6; i++)
                {
                    if (codes[i].ToString().StartsWith("nop NULL")// 53
                        && codes[i + 9].ToString().StartsWith("call static float UnityEngine.Vector3::Distance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)"))// 62
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes.Insert(0, new CodeInstruction(OpCodes.Br, codes[startIndex].labels[0]));
                    startIndex = -1;
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.SyncBodyPositionClientRpc_ReversePatch could not bypass rpc stuff");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("SetSpecialGrabAnimationBool")]
        [HarmonyReversePatch]
        public static void SetSpecialGrabAnimationBool_ReversePatch(object instance, bool setTrue, GrabbableObject currentItem) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.SetSpecialGrabAnimationBool_ReversePatch");

        #endregion

        #region Transpilers

        [HarmonyPatch("SpectateNextPlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SpectateNextPlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            if (!PluginRuntimeProvider.Context.Config.CanSpectateInterns)
            {
                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                        && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                        && codes[i + 2].ToString() == "ldlen NULL")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex].operand = null;
                    codes[startIndex + 1].opcode = OpCodes.Nop;
                    codes[startIndex + 1].operand = null;
                    codes[startIndex + 2].opcode = OpCodes.Call;
                    codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                    startIndex = -1;
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.SpectateNextPlayer_Transpiler could not use irl number of player for iteration.");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                        && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                        && codes[i + 2].ToString() == "ldlen NULL")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex].operand = null;
                    codes[startIndex + 1].opcode = OpCodes.Nop;
                    codes[startIndex + 1].operand = null;
                    codes[startIndex + 2].opcode = OpCodes.Call;
                    codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                    startIndex = -1;
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.SpectateNextPlayer_Transpiler could not use irl number of player.");
                }
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("ConnectClientToPlayerObject")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ConnectClientToPlayerObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL")
                    && codes[i + 1].ToString() == "ldfld StartOfRound GameNetcodeStuff.PlayerControllerB::playersManager"
                    && codes[i + 2].ToString() == "ldfld UnityEngine.GameObject[] StartOfRound::allPlayerObjects"
                    && codes[i + 3].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Nop;
                codes[startIndex + 2].operand = null;
                codes[startIndex + 3].opcode = OpCodes.Call;
                codes[startIndex + 3].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ConnectClientToPlayerObject_Transpiler could not limit teleport to only not interns.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("Interact_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Interact_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 28; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL")
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 28].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + 28].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.Interact_performed_Transpiler could not ignore intract input when commands opened");
            }

            return codes.AsEnumerable();
        }

        // [HarmonyPatch("Jump_performed")] cannot patch this one, why ?
        [HarmonyPatch("ItemSecondaryUse_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ItemSecondaryUse_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            int indexJumpTo = 71;

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - indexJumpTo; i++)
            {
                if (codes[i + indexJumpTo].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + indexJumpTo].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ItemSecondaryUse_performed could not ignore ItemSecondaryUse_performed input when commands opened");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("ItemTertiaryUse_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ItemTertiaryUse_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            int indexJumpTo = 57;

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - indexJumpTo; i++)
            {
                if (codes[i + indexJumpTo].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + indexJumpTo].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ItemTertiaryUse_performed could not ignore ItemTertiaryUse_performed input when commands opened");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("QEItemInteract_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> QEItemInteract_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            int indexJumpTo = 68;

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - indexJumpTo; i++)
            {
                if (codes[i + indexJumpTo].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + indexJumpTo].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.QEItemInteract_performed could not ignore QEItemInteract_performed input when commands opened");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("InspectItem_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> InspectItem_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            int indexJumpTo = 52;

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - indexJumpTo; i++)
            {
                if (codes[i + indexJumpTo].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + indexJumpTo].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.InspectItem_performed could not ignore InspectItem_performed input when commands opened");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("ScrollMouse_performed")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ScrollMouse_performed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            int indexJumpTo = 59;

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - indexJumpTo; i++)
            {
                if (codes[i + indexJumpTo].ToString().StartsWith("ret NULL"))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsAnyMenuOpenedMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + indexJumpTo].labels[0])
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.ScrollMouse_performed could not ignore ScrollMouse_performed input when commands opened");
            }

            return codes.AsEnumerable();
        }

        #endregion

        #region Postfixes

        [HarmonyPatch("Start")]
        [HarmonyAfter(Const.MOREEMOTES_GUID)]
        [HarmonyPostfix]
        static void Start_PostFix(PlayerControllerB __instance, ref Collider[] ___nearByPlayers)
        {
            ___nearByPlayers = new Collider[InternManagerProvider.Instance.AllEntitiesCount];
        }

        /// <summary>
        /// Debug patch to spawn an intern at will
        /// </summary>
        [HarmonyPatch("PerformEmote")]
        [HarmonyPostfix]
        static void PerformEmote_PostFix(PlayerControllerB __instance)
        {
            if (!DebugConst.SPAWN_INTERN_WITH_EMOTE)
            {
                return;
            }

            if (__instance.playerUsername != "Player #0")
            {
                return;
            }

            int identityID = -1;
            int[] selectedIdentities = IdentityManagerProvider.Instance.GetIdentitiesToDrop();
            if (selectedIdentities.Length > 0)
            {
                identityID = selectedIdentities[0];
            }

            if (identityID < 0)
            {
                identityID = IdentityManagerProvider.Instance.GetNewIdentityToSpawn();
            }

            InternManagerProvider.Instance.SpawnThisInternServerRpc(identityID, new SpawnInternsParamsNetworkSerializable()
            {
                enumSpawnAnimation = (int)EnumSpawnAnimation.None,
                SpawnPosition = __instance.transform.position,
                YRot = __instance.transform.eulerAngles.y,
                IsOutside = !__instance.isInsideFactory
            });
        }

        /// <summary>
        /// Patch to add text when pointing at an intern at grab range,<br/>
        /// shows the different possible actions for interacting with intern
        /// </summary>
        [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        static void SetHoverTipAndCurrentInteractTrigger_PostFix(ref PlayerControllerB __instance,
                                                                 ref Ray ___interactRay,
                                                                 int ___playerMask,
                                                                 int ___interactableObjectsMask,
                                                                 ref RaycastHit ___hit)
        {
            ___interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (Physics.Raycast(___interactRay, out ___hit, __instance.grabDistance, ___interactableObjectsMask) && ___hit.collider.gameObject.layer != 8 && ___hit.collider.gameObject.layer != 30)
            {
                // Check if we are pointing to a ragdoll body of intern (not grabbable)
                if (___hit.collider.tag == "PhysicsProp")
                {
                    RagdollGrabbableObject? ragdoll = ___hit.collider.gameObject.GetComponent<RagdollGrabbableObject>();
                    if (ragdoll == null)
                    {
                        return;
                    }

                    if (ragdoll.bodyID == Const.INIT_RAGDOLL_ID)
                    {
                        // Remove tooltip text
                        __instance.cursorTip.text = string.Empty;
                        __instance.cursorIcon.enabled = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// See <see cref="StopHoldInteractionOnTrigger_PreFix"><c>StopHoldInteractionOnTrigger_PreFix</c></see>
        /// </summary>
        /// <param name="__state"></param>
        //[HarmonyPatch("StopHoldInteractionOnTrigger")]
        //[HarmonyPostfix]
        //static void StopHoldInteractionOnTrigger_PostFix(float __state)
        //{
        //    if (InputManager.Instance.OpenCommandsInternInputIsPressed)
        //    {
        //        HUDManager.Instance.holdFillAmount = __state;
        //    }
        //}

        #endregion
    }
}
