using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using OPJosMod.ReviveCompany;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.InputSystem;
using Label = System.Reflection.Emit.Label;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LethalInternship.Patches.NpcPatches
{
    /// <summary>
    /// Patch for <c>PlayerControllerB</c>
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI == null)
            {
                if ((int)__instance.playerClientId >= InternManager.Instance.IndexBeginOfInterns)
                {
                    return false;
                }

                return true;
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

            internAI.NpcController.Update();

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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                return false;
            }
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"SyncDamageIntern called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.SyncDamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
                return false;
            }

            if (Const.INVULNERABILITY)
            {
                // Bootleg invulnerability
                Plugin.LogDebug($"Bootleg invulnerability (return false)");
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
            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"SyncDamageInternFromOtherClient called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.DamageInternFromOtherClientServerRpc(damageAmount, hitDirection, playerWhoHit);
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"SyncKillIntern called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, Intern #{internAI.InternId}");
                internAI.SyncKillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
                return false;
            }

            if (Const.INVINCIBILITY)
            {
                // Bootleg invincibility
                Plugin.LogDebug($"Bootleg invincibility");
                return false;
            }

            // Check if we hold interns
            InternAI[] internsAIsHoldByPlayer = InternManager.Instance.GetInternsAiHoldByPlayer((int)__instance.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                InternAI internAIHeld;
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internAIHeld = internsAIsHoldByPlayer[i];

                    // Release interns
                    internAIHeld.SyncReleaseIntern(__instance);
                    internAIHeld.SyncTeleportIntern(__instance.transform.position, !__instance.isInsideFactory, isUsingEntrance: false);

                    switch (causeOfDeath)
                    {
                        case CauseOfDeath.Gravity:
                        case CauseOfDeath.Blast:
                        case CauseOfDeath.Suffocation:
                        case CauseOfDeath.Drowning:
                        case CauseOfDeath.Abandoned:
                            Plugin.LogDebug($"SyncKillIntern on held intern on LOCAL client #{internAIHeld.NetworkManager.LocalClientId}, Intern #{internAIHeld.InternId}");
                            internAIHeld.SyncKillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
                            break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Patch for intercepting the discard action from the player,
        /// if he is looking at an intern, orders him to drop his item
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("Discard_performed")]
        [HarmonyPrefix]
        static bool Discard_performed_PreFix(PlayerControllerB __instance,
                                             float ___timeSinceSwitchingSlots,
                                             bool ___throwingObject,
                                             ref InputAction.CallbackContext context,
                                             ref Ray ___interactRay,
                                             ref int ___playerMask)
        {
            if (!__instance.IsOwner || !__instance.isPlayerControlled)
            {
                return true;
            }

            if (!context.performed)
            {
                return true;
            }

            // Make an intern drop his object
            RaycastHit[] raycastHits = Physics.RaycastAll(___interactRay, __instance.grabDistance, ___playerMask);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                if (!intern.AreHandsFree())
                {
                    // Intern drop item
                    intern.DropItemServerRpc();
                }
                else if (__instance.currentlyHeldObjectServer != null)
                {
                    // Intern take item from player hands
                    GrabbableObject grabbableObject = __instance.currentlyHeldObjectServer;
                    __instance.DiscardHeldObject(placeObject: true);
                    intern.GrabItemServerRpc(grabbableObject.NetworkObject, itemGiven: true);
                }

                return false;
            }

            if (___timeSinceSwitchingSlots < 0.2f || __instance.isGrabbingObjectAnimation || __instance.isTypingChat || __instance.inSpecialInteractAnimation)
            {
                return true;
            }
            if (__instance.activatingItem)
            {
                return true;
            }
            if (___throwingObject || !__instance.isHoldingObject || __instance.currentlyHeldObjectServer == null)
            {
                return true;
            }

            Plugin.LogDebug($"player dropped {__instance.currentlyHeldObjectServer}");
            InternAI.DictJustDroppedItems[__instance.currentlyHeldObjectServer] = Time.realtimeSinceStartup;
            return true;
        }

        /// <summary>
        /// Patch for intercepting the interact action from the player,
        /// if he is looking at an intern, orders to follow him and change his ownership
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch("Interact_performed")]
        [HarmonyPrefix]
        static bool Interact_performed_PreFix(PlayerControllerB __instance,
                                              ref Ray ___interactRay,
                                              ref int ___playerMask)
        {
            if (!__instance.IsOwner || !__instance.isPlayerControlled)
            {
                return true;
            }

            // Use of interact key to assign intern to player
            RaycastHit[] raycastHits = Physics.RaycastAll(___interactRay, __instance.grabDistance, ___playerMask);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                if (intern.OwnerClientId != __instance.actualClientId)
                {
                    intern.SyncAssignTargetAndSetMovingTo(__instance);
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch("ItemSecondaryUse_performed")]
        [HarmonyPrefix]
        private static bool ItemSecondaryUse_performed_PreFix(PlayerControllerB __instance,
                                                              ref Ray ___interactRay,
                                                              ref int ___playerMask)
        {
            if (!__instance.IsOwner || !__instance.isPlayerControlled)
            {
                return true;
            }

            // Use of secondary use key (A) to grab intern
            RaycastHit[] raycastHits = Physics.RaycastAll(___interactRay, __instance.grabDistance, ___playerMask);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                intern.SyncAssignTargetAndSetMovingTo(__instance);
                // Grab intern
                intern.GrabInternServerRpc(__instance.playerClientId);

                return false;
            }

            // No intern in interact range
            // Check if we hold interns
            InternAI[] internsAIsHoldByPlayer = InternManager.Instance.GetInternsAiHoldByPlayer((int)__instance.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                InternAI internAI;
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internAI = internsAIsHoldByPlayer[i];
                    internAI.SyncReleaseIntern(__instance);
                    internAI.SyncTeleportIntern(__instance.transform.position, !__instance.isInsideFactory, isUsingEntrance: false);
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch("ItemTertiaryUse_performed")]
        [HarmonyPrefix]
        private static bool ItemTertiaryUse_performed_PreFix(PlayerControllerB __instance,
                                                             InputAction.CallbackContext context)
        {
            if (!__instance.IsOwner || !__instance.isPlayerControlled)
            {
                return true;
            }

            // Nothing here for now

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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
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

                InternAI? internAI = InternManager.Instance.GetInternAiOwnerOfObject(grabbableObject);
                if (internAI == null)
                {
                    // Quit and continue original method
                    Plugin.LogDebug($"no intern found who hold item {grabbableObject.name}");
                    return true;
                }

                Plugin.LogDebug($"intern {internAI.NpcController.Npc.playerUsername} drop item {grabbableObject.name} before grab by player");
                grabbableObject.isHeld = false;
                internAI.DropItemServerRpc();
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"NetworkManager {__instance.NetworkManager}, newBodyPosition {newBodyPosition}, this.deadBody {__instance.deadBody}");
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
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
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region reverse patches

        /// <summary>
        /// Reverse patch modified, makes the code ignore some condition if is intern, <br/>
        /// and call the right method for jumping with an intern
        /// </summary>
        [HarmonyPatch("Jump_performed")]
        [HarmonyReversePatch]
        public static void JumpPerformed_ReversePatch(object instance, InputAction.CallbackContext context)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = Transpilers.Manipulator(instructions,
                                                                      item => item.opcode == OpCodes.Ldarg_1,
                                                                      item => item.opcode = OpCodes.Ldarg_0
                                                                      ).ToList();

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 21; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //0
                        && codes[i + 1].ToString().StartsWith("ldfld QuickMenuManager GameNetcodeStuff.PlayerControllerB::quickMenuManager")
                        && codes[i + 2].ToString().StartsWith("ldfld bool QuickMenuManager::isMenuOpen")
                        && codes[i + 3].ToString().StartsWith("brfalse")
                        && codes[i + 4].ToString().StartsWith("ret NULL")
                        && codes[i + 21].ToString().StartsWith("ldarg.0 NULL")) // 21
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 21; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove all condition until inSpecialInteractAnimation condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 3; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 26
                        && codes[i + 1].ToString().StartsWith("ldfld bool GameNetcodeStuff.PlayerControllerB::isTypingChat")
                        && codes[i + 2].ToString().StartsWith("brfalse")
                        && codes[i + 3].ToString().StartsWith("ret NULL"))
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 4; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove isTypingChat condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 101
                        && codes[i + 1].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::PlayerJumpedServerRpc()"))
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 1].operand = PatchesUtil.SyncJumpMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not use jump method for intern");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <summary>
        /// Reverse patch to call <c>PlayJumpAudio</c>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("PlayJumpAudio")]
        [HarmonyReversePatch]
        public static void PlayJumpAudio_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayJumpAudio_ReversePatch");

        /// <summary>
        /// Reverse patch to call <c>CalculateGroundNormal</c>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("CalculateGroundNormal")]
        [HarmonyReversePatch]
        public static void CalculateGroundNormal_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchCalculateGroundNormal_ReversePatch");

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
                for (var i = 0; i < codes.Count - 5; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 33
                        && codes[i + 5].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::LandFromJumpServerRpc(")) // 38
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 5].operand = PatchesUtil.SyncLandFromJumpMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch could not use jump from land method for intern");
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

        /// <summary>
        /// Reverse patch to call <c>NearOtherPlayers</c>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("NearOtherPlayers")]
        [HarmonyReversePatch]
        public static bool NearOtherPlayers_ReversePatch(object instance, PlayerControllerB playerScript, float checkRadius) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.NearOtherPlayers_ReversePatch");

        /// <summary>
        /// Reverse patch modified to update player position client side for the intern
        /// </summary>
        /// <remarks>
        /// Bypassing all rpc condition, because the intern is not owner of his body, no one is, the body <c>PlayerControllerB</c> of intern is not spawned.<br/>
        /// Changind isOwner with a call to know if the controller is an intern owned by a the client player executing the code
        /// </remarks>
        [HarmonyPatch("UpdatePlayerPositionClientRpc")]
        [HarmonyReversePatch]
        public static void UpdatePlayerPositionClientRpc_ReversePatch(object instance, Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].ToString().StartsWith("stfld int EndOfGameStats::allStepsTaken"))// 98
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    // Removing rpc stuff not working, only bypass
                    Label label = generator.DefineLabel();
                    codes[startIndex + 1].labels.Add(label);
                    codes.Insert(0, new CodeInstruction(OpCodes.Br, label));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not bypass all beginning with rpc stuff");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 22; i++)
                {
                    if (codes[i].ToString().StartsWith("call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()") // 104
                        && codes[i + 22].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::DropBlood(UnityEngine.Vector3 direction, bool leaveBlood, bool leaveFootprint)"))// 126
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Call;
                    codes[startIndex].operand = PatchesUtil.IsPlayerInternOwnerLocalMethod;
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not change is owner with is intern and owner is local");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].ToString().StartsWith("call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()") // 131
                        && codes[i + 2].ToString().StartsWith("ret NULL"))// 133
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Call;
                    codes[startIndex].operand = PatchesUtil.IsPlayerInternOwnerLocalMethod;
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not change is owner with is intern and owner is local 2");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 14; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 206
                        && codes[i + 1].ToString().StartsWith("ldfld bool GameNetcodeStuff.PlayerControllerB::isInElevator")//207
                        && codes[i + 14].ToString().StartsWith("callvirt void UnityEngine.Transform::SetParent(UnityEngine.Transform p)"))// 220
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (int i = startIndex; i < codes.Count - 1; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not remove all end stuff with inElevator (intern controller is never server because not spawned)");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <summary>
        /// Reverse patch modified to call the right client rpc method for an intern
        /// </summary>
        [HarmonyPatch("UpdatePlayerAnimationsToOtherClients")]
        [HarmonyReversePatch]
        public static void UpdatePlayerAnimationsToOtherClients_ReversePatch(object instance, Vector2 moveInputVector)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 7; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL")// 57
                        && codes[i + 7].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)"))// 64
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 7].operand = PatchesUtil.UpdatePlayerAnimationServerRpcMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not use own update animation rpc method 1");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL")// 84
                        && codes[i + 4].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)"))// 88
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 4].operand = PatchesUtil.UpdatePlayerAnimationServerRpcMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not use own update animation rpc method 2");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <summary>
        /// Reverse patch to be able to call <c>UpdatePlayerAnimationClientRpc</c>
        /// </summary>
        /// <remarks>
        /// Bypassing all rpc condition, because the intern is not owner of his body, no one is, the body <c>PlayerControllerB</c> of intern is not spawned.<br/>
        /// </remarks>
        [HarmonyPatch("UpdatePlayerAnimationClientRpc")]
        [HarmonyReversePatch]
        public static void UpdatePlayerAnimationClientRpc_ReversePatch(object instance, int animationState, float animationSpeed)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 3; i++)
                {
                    if (codes[i].ToString().StartsWith("call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()")// 61
                        && codes[i + 3].ToString().StartsWith("ldarg.0 NULL"))// 64
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
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerAnimationClientRpc_ReversePatch could not bypass rpc stuff");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

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
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch could not bypass rpc stuff");
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
                        && codes[i + 6].ToString().StartsWith("call static float UnityEngine.Vector3::Distance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)"))// 59
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
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.SyncBodyPositionClientRpc_ReversePatch could not bypass rpc stuff");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        #endregion

        #region Postfixes

        /// <summary>
        /// Debug patch to spawn an intern at will
        /// </summary>
        [HarmonyPatch("PerformEmote")]
        [HarmonyPostfix]
        static void PerformEmote_PostFix(PlayerControllerB __instance)
        {
            if (!Const.SPAWN_INTERN_WITH_EMOTE)
            {
                return;
            }

            if (__instance.playerUsername != "Player #0")
            {
                return;
            }

            InternManager.Instance.SpawnInternServerRpc(__instance.transform.position, __instance.transform.eulerAngles.y, !__instance.isInsideFactory);
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

                    InternAI? internAI = InternManager.Instance.GetInternAI(ragdoll.bodyID.Value);
                    if (internAI != null 
                        && internAI.RagdollInternBody != null
                        && internAI.RagdollInternBody.IsRagdollBodyHeldByPlayer((int)__instance.playerClientId))
                    {
                        // Remove tooltip text
                        __instance.cursorTip.text = string.Empty;
                        __instance.cursorIcon.enabled = false;
                        return;
                    }
                }
            }

            // Set tooltip when pointing at intern
            RaycastHit[] raycastHits = Physics.RaycastAll(___interactRay, __instance.grabDistance, ___playerMask);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }

                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                // Name billboard
                intern.NpcController.ShowFullNameBillboard();

                StringBuilder sb = new StringBuilder();
                // Line item
                if (!intern.AreHandsFree())
                {
                    sb.Append("Drop your item : [G]").AppendLine();
                }
                else if (__instance.currentlyHeldObjectServer != null)
                {
                    sb.Append("Take my item : [G]").AppendLine();
                }

                // Line Follow
                if (intern.OwnerClientId != __instance.actualClientId)
                {
                    sb.Append("Follow me: [E]").AppendLine();
                }

                // Grab intern
                sb.Append("Grab intern: [Q]");

                __instance.cursorTip.text = sb.ToString();

                break;
            }
        }

        [HarmonyPatch("IVisibleThreat.GetThreatTransform")]
        [HarmonyPostfix]
        static void GetThreatTransform_PostFix(PlayerControllerB __instance, ref Transform __result)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                __result = internAI.transform;
            }
        }

        #endregion
    }
}
