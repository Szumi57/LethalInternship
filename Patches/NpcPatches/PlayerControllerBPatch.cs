using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem.PlaybackState;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LethalInternship.Patches.NpcPatches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        #region Prefixes

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(PlayerControllerB __instance,
                                  ref bool ___isCameraDisabled,
                                  bool ___isJumping,
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

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool Awake_PreFix(PlayerControllerB __instance,
                                 ref bool ___isCameraDisabled,
                                 ref Vector3 ___rightArmProceduralTargetBasePosition,
                                 ref int ___previousAnimationState)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                //internAI.NpcController.IsCameraDisabled = ___isCameraDisabled;
                //internAI.NpcController.RightArmProceduralTargetBasePosition = ___rightArmProceduralTargetBasePosition;
                //internAI.NpcController.PreviousAnimationState = ___previousAnimationState;

                //internAI.NpcController.Awake();

                //___isCameraDisabled = internAI.NpcController.IsCameraDisabled;
                //___rightArmProceduralTargetBasePosition = internAI.NpcController.RightArmProceduralTargetBasePosition;
                //___previousAnimationState = internAI.NpcController.PreviousAnimationState;
                return false;
            }
            return true;
        }

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
                Plugin.Logger.LogDebug($"SyncDamageIntern called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.SyncDamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
                return false;
            }

            // todo: Bootleg invulnerability
            //Plugin.Logger.LogDebug($"Bootleg invulnerability (return false)");
            //return false;
            return true;
        }

        [HarmonyPatch("DamagePlayerFromOtherClientServerRpc")]
        [HarmonyPrefix]
        static bool DamagePlayerFromOtherClientServerRpc_PreFix(PlayerControllerB __instance,
                                                                int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.Logger.LogDebug($"SyncDamageInternFromOtherClient called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.DamageInternFromOtherClientServerRpc(damageAmount, hitDirection, playerWhoHit);
                return false;
            }

            return true;
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayer_PreFix(PlayerControllerB __instance,
                                      Vector3 bodyVelocity,
                                      bool spawnBody,
                                      CauseOfDeath causeOfDeath,
                                      int deathAnimation)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.Logger.LogDebug($"SyncKillIntern called from game code on LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.SyncKillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
                return false;
            }

            // todo: Bootleg invincibility
            Plugin.Logger.LogDebug($"Bootleg invincibility (return false)");
            return false;
            return true;
        }

        [HarmonyPatch("Discard_performed")]
        [HarmonyPrefix]
        static bool Discard_performed_PreFix(PlayerControllerB __instance,
                                             float ___timeSinceSwitchingSlots,
                                             bool ___throwingObject,
                                             ref InputAction.CallbackContext context,
                                             ref Ray ___interactRay,
                                             ref int ___playerMask)
        {
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
                    intern.GrabItemServerRpc(grabbableObject.NetworkObject);
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

            Plugin.Logger.LogDebug($"player dropped {__instance.currentlyHeldObjectServer}");
            InternAI.DictJustDroppedItems[__instance.currentlyHeldObjectServer] = Time.realtimeSinceStartup;
            return true;
        }

        [HarmonyPatch("Interact_performed")]
        [HarmonyPrefix]
        static bool Interact_performed_PreFix(PlayerControllerB __instance,
                                              ref Ray ___interactRay,
                                              ref int ___playerMask)
        {
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

        [HarmonyPatch("UpdateSpecialAnimationValue")]
        [HarmonyPrefix]
        static bool UpdateSpecialAnimationValue_PreFix(PlayerControllerB __instance,
                                                       bool specialAnimation, short yVal, float timed, bool climbingLadder)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                internAI.UpdateSpecialAnimationValue(specialAnimation, yVal, timed, climbingLadder);
                return false;
            }

            return true;
        }

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

                InternAI? internAI = InternManager.Instance.GetInternAiObjectOwnerOf(grabbableObject);
                if (internAI == null)
                {
                    // Quit and continue original method
                    Plugin.Logger.LogDebug($"no intern found who hold item {grabbableObject}");
                    return true;
                }

                Plugin.Logger.LogDebug($"intern drop item before grab by player");
                grabbableObject.isHeld = false;
                internAI.DropItemServerRpc();
            }

            return true;
        }

        [HarmonyPatch("SyncBodyPositionClientRpc")]
        [HarmonyPrefix]
        static bool SyncBodyPositionClientRpc_PreFix(PlayerControllerB __instance, Vector3 newBodyPosition)
        {
            // send to server if intern from controller
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.playerClientId);
            if (internAI != null)
            {
                Plugin.Logger.LogDebug($"NetworkManager {__instance.NetworkManager}, newBodyPosition {newBodyPosition}, this.deadBody {__instance.deadBody}");
                internAI.SyncDeadBodyPositionServerRpc(newBodyPosition);
                return false;
            }

            return true;
        }

        #endregion

        #region Transpilers

        //[HarmonyPatch("DamagePlayer")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DamagePlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // remove all HUDelement udpates and all that stuff if player is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString().StartsWith("call static HUDManager HUDManager::get_Instance()")
                    && codes[i + 4].ToString() == "callvirt void HUDManager::UpdateHealthUI(int health, bool hurtPlayer)")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 5/*IL_0063: ldarg.0*/);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchDamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::ShakeCamera.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()"
                    && codes[i + 2].ToString() == "callvirt void HUDManager::ShakeCamera(ScreenShakeType shakeType)")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 3/*IL_009B: ldarg.0*/);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchDamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::UpdateHealthUI.");
            }


            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld UnityEngine.AudioSource HUDManager::UIAudio"
                    && codes[i + 3].ToString() == "ldfld UnityEngine.AudioClip StartOfRound::fallDamageSFX")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 12/*IL_0130: ldarg.0*/);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchDamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::UIAudio and WalkieTalkie.TransmitOneShotAudio.");
            }


            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld UnityEngine.AudioSource HUDManager::UIAudio"
                    && codes[i + 3].ToString() == "ldfld UnityEngine.AudioClip StartOfRound::damageSFX")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 9/*IL_0168: ldarg.0*/);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatch.DamagePlayer_Transpiler could not insert instruction if is intern for AudioSource::PlayOneShot and StartOfRound::LocalPlayerDamagedEvent.");
            }


            // ----------------------------------------------------------------------
            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"DamagePlayer ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DamagePlayer ======================");
            return codes.AsEnumerable();
        }

        //[HarmonyPatch("KillPlayer")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // remove all HUDelement udpates and all that stuff if player is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()" //65
                    && codes[i + 3].ToString() == "call static HUDManager HUDManager::get_Instance()")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 6);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayer_Transpiler could not insert instruction if is intern for StartOfRound::get_Instance() and HUDManager::get_Instance().");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 35; i++)
            {
                if (codes[i].ToString() == "call static Terminal UnityEngine.Object::FindObjectOfType()" //89
                    && codes[i + 8].ToString() == "call void GameNetcodeStuff.PlayerControllerB::ChangeAudioListenerToObject(UnityEngine.GameObject addToObject)"
                    && codes[i + 12].ToString() == "callvirt void SoundManager::SetDiageticMixerSnapshot(int snapshotID, float transitionTime)"
                    && codes[i + 15].ToString() == "callvirt void HUDManager::SetNearDepthOfFieldEnabled(bool enabled)"
                    && codes[i + 18].ToString() == "ldstr \"biohazardDamage\""
                    && codes[i + 21].ToString() == "ldstr \"Running kill player function for LOCAL client, player object: \""
                    && codes[i + 29].ToString() == "ldstr \"gameOver\""
                    && codes[i + 33].ToString() == "callvirt void HUDManager::HideHUD(bool hide)"
                    && codes[i + 35].ToString() == "call void GameNetcodeStuff.PlayerControllerB::StopHoldInteractionOnTrigger()")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 36);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayer_Transpiler could not insert instruction if is intern for FindObjectOfType and others.");
            }


            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 12; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance() [Label4]"//146
                    && codes[i + 3].ToString() == "callvirt void StartOfRound::SwitchCamera(UnityEngine.Camera newCamera)"
                    && codes[i + 6].ToString() == "stfld float GameNetcodeStuff.PlayerControllerB::isInGameOverAnimation"
                    && codes[i + 9].ToString() == "ldstr \"\""
                    && codes[i + 12].ToString() == "ldfld UnityEngine.UI.Image GameNetcodeStuff.PlayerControllerB::cursorIcon")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 15);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayer_Transpiler could not insert instruction if is intern for SwitchCamera and others.");
            }

            // ----------------------------------------------------------------------
            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"KillPlayer ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"KillPlayer ======================");
            return codes.AsEnumerable();
        }

        //[HarmonyPatch("KillPlayerServerRpc")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerServerRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //89
                    && codes[i + 1].ToString() == "ldfld StartOfRound GameNetcodeStuff.PlayerControllerB::playersManager"
                    && codes[i + 2].ToString() == "dup NULL"
                    && codes[i + 3].ToString() == "ldfld int StartOfRound::livingPlayers"
                    && codes[i + 4].ToString() == "ldc.i4.1 NULL"
                    && codes[i + 5].ToString() == "sub NULL"
                    && codes[i + 6].ToString() == "stfld int StartOfRound::livingPlayers")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 7);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayerServerRpc_Transpiler could not insert instruction if is intern for livingPlayers--.");
            }


            // ----------------------------------------------------------------------
            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"KillPlayerServerRpc ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"KillPlayerServerRpc ======================");
            return codes.AsEnumerable();
        }

        //[HarmonyPatch("KillPlayerClientRpc")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 11; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //82
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsServer()"
                    && codes[i + 2].ToString() == "brtrue Label9"
                    && codes[i + 6].ToString() == "ldfld StartOfRound GameNetcodeStuff.PlayerControllerB::playersManager"
                    && codes[i + 7].ToString() == "dup NULL"
                    && codes[i + 8].ToString() == "ldfld int StartOfRound::livingPlayers"
                    && codes[i + 9].ToString() == "ldc.i4.1 NULL"
                    && codes[i + 10].ToString() == "sub NULL"
                    && codes[i + 11].ToString() == "stfld int StartOfRound::livingPlayers")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 29);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayerClientRpc_Transpiler could not insert instruction if is intern for livingPlayers-- after isServer.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString() == "call static SoundManager SoundManager::get_Instance()" //178
                    && codes[i + 1].ToString() == "ldfld float[] SoundManager::playerVoicePitchTargets"
                    && codes[i + 5].ToString() == "call static SoundManager SoundManager::get_Instance()"
                    && codes[i + 6].ToString() == "ldfld float[] SoundManager::playerVoicePitchLerpSpeed")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 10);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayerClientRpc_Transpiler could not insert instruction if is intern for playerVoicePitchTargets, playerVoicePitchLerpSpeed.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 6; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //191
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 2].ToString() == "brtrue Label12"
                    && codes[i + 5].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::isPlayerDead"
                    && codes[i + 8].ToString() == "callvirt void HUDManager::UpdateBoxesSpectateUI()"
                    && codes[i + 10].ToString() == "callvirt void StartOfRound::UpdatePlayerVoiceEffects()")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 11);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchKillPlayerClientRpc_Transpiler could not insert instruction if is intern for UpdateBoxesSpectateUI(),  UpdatePlayerVoiceEffects().");
            }


            // ----------------------------------------------------------------------
            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"KillPlayerClientRpc ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"KillPlayerClientRpc ======================");
            return codes.AsEnumerable();
        }

        //[HarmonyPatch("SwitchToItemSlot")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SwitchToItemSlot_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 7; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //3
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 7].ToString() == "ldfld UnityEngine.UI.Image[] HUDManager::itemSlotIconFrames") //10
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 32); //35
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchSwitchToItemSlot_Transpiler could not remove hudmanager itemsloticonframes");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 10; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //44
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 10].ToString() == "callvirt void UnityEngine.UI.Image::set_sprite(UnityEngine.Sprite value)") //54
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 18); //62
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchSwitchToItemSlot_Transpiler could not remove hudmanager set_sprite");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL [Label16, Label17]" //215
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 4].ToString() == "callvirt void HUDManager::ClearControlTips()") //219
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 5); //220
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchSwitchToItemSlot_Transpiler could not remove hudmanager clearcontroltips");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 14; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL [Label15]" //237
                    && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()"
                    && codes[i + 14].ToString() == "ldfld TMPro.TextMeshProUGUI HUDManager::holdingTwoHandedItem") //251
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 29); //266
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchSwitchToItemSlot_Transpiler could not remove hudmanager holdingTwoHandedItem");
            }

            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");

            return codes.AsEnumerable();
        }

        //[HarmonyPatch("DiscardHeldObject")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DiscardHeldObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()" //14
                    && codes[i + 1].ToString() == "ldfld UnityEngine.UI.Image[] HUDManager::itemSlotIcons"
                    && codes[i + 8].ToString() == "ldfld TMPro.TextMeshProUGUI HUDManager::holdingTwoHandedItem") //22
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 11); //25
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchDiscardHeldObject_Transpiler could not remove HUDManager::itemSlotIcons");
            }

            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");

            return codes.AsEnumerable();
        }

        //[HarmonyPatch("DropAllHeldItems")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DropAllHeldItems_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"DropAllHeldItems ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DropAllHeldItems ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 11; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()" //97
                    && codes[i + 1].ToString() == "ldfld TMPro.TextMeshProUGUI HUDManager::holdingTwoHandedItem"
                    && codes[i + 5].ToString() == "ldfld UnityEngine.UI.Image[] HUDManager::itemSlotIcons" //102
                    && codes[i + 11].ToString() == "callvirt void HUDManager::ClearControlTips()") //108
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 12); //109
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchDropAllHeldItems_Transpiler could not remove HUDManager::holdingTwoHandedItem, itemSlotIcons, ClearControlTips");
            }

            //Plugin.Logger.LogDebug($"DropAllHeldItems ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DropAllHeldItems ======================");

            return codes.AsEnumerable();
        }

        #endregion

        #region reverse patches

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove all condition until inSpecialInteractAnimation condition");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove isTypingChat condition");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not use jump method for intern");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("PlayJumpAudio")]
        [HarmonyReversePatch]
        public static void PlayJumpAudio_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayJumpAudio_ReversePatch");

        [HarmonyPatch("Discard_performed")]
        [HarmonyReversePatch]
        public static void Discard_performed_ReversePatch(object instance, InputAction.CallbackContext context)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = Transpilers.Manipulator(instructions,
                                                                      item => item.opcode == OpCodes.Ldarg_1,
                                                                      item => item.opcode = OpCodes.Ldarg_0
                                                                      ).ToList();

                //Plugin.Logger.LogDebug($"Discard_performed ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"Discard_performed ======================");

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //6
                        && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsServer()"
                        && codes[i + 2].ToString() == "brfalse Label3"
                        && codes[i + 3].ToString() == "ldarg.0 NULL"
                        && codes[i + 4].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::isHostPlayerObject")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 6; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    codes[startIndex].opcode = OpCodes.Br;
                    codes[startIndex].operand = codes[startIndex + 11].labels[0];
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchDiscard_performed_ReversePatch could not remove isHostPlayerObject condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarga.s 1") //13
                        && codes[i + 1].ToString() == "call bool UnityEngine.InputSystem.InputAction+CallbackContext::get_performed()")
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
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchDiscard_performed_ReversePatch could not remove action performed condition");
                }

                //Plugin.Logger.LogDebug($"Discard_performed ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"Discard_performed ======================");
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("CalculateGroundNormal")]
        [HarmonyReversePatch]
        public static void CalculateGroundNormal_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchCalculateGroundNormal_ReversePatch");

        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyReversePatch]
        public static void PlayerHitGroundEffects_ReversePatch(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 5; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 33
                        && codes[i + 5].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::LandFromJumpServerRpc(bool fallHard)")) // 38
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch could not use jump from land method for intern");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("CheckConditionsForEmote")]
        [HarmonyReversePatch]
        public static bool CheckConditionsForEmote_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerControllerBPatchCheckConditionsForEmote_ReversePatch");

        [HarmonyPatch("LateUpdate")]
        [HarmonyReversePatch]
        public static void LateUpdate_ReversePatch(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = Transpilers.Manipulator(instructions,
                                                                      item => item.opcode == OpCodes.Ldarg_1,
                                                                      item => item.opcode = OpCodes.Ldarg_0
                                                                      ).ToList();

                //Plugin.Logger.LogDebug($"LateUpdate ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"LateUpdate ======================");

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 5; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //53
                        && codes[i + 1].ToString() == "ldarg.0 NULL"
                        && codes[i + 5].ToString() == "stfld UnityEngine.Vector3 GameNetcodeStuff.PlayerControllerB::previousElevatorPosition")//58
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes.RemoveRange(0, startIndex);
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove all beginning with in elevator stuff");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()" //68
                        && codes[i + 1].ToString().StartsWith("brtrue"))
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 1].opcode = OpCodes.Brfalse;
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not use is owner for displaying names above interns");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 10; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //104
                        && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsOwner()" //105
                        && codes[i + 10].ToString() == "call void GameNetcodeStuff.PlayerControllerB::PlayerLookInput()")// 114
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 1].opcode = OpCodes.Call;
                    codes[startIndex + 1].operand = PatchesUtil.IsPlayerInternOwnerLocalMethod;
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.LateUpdate_ReversePatch could not change check if owner");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL")// 113
                        && codes[i + 1].ToString() == "call void GameNetcodeStuff.PlayerControllerB::PlayerLookInput()")// 114
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
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove lookinput");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 21; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //210
                        && codes[i + 1].ToString() == "ldfld UnityEngine.Transform GameNetcodeStuff.PlayerControllerB::localVisor"
                        && codes[i + 21].ToString() == "callvirt void UnityEngine.Transform::set_rotation(UnityEngine.Quaternion value)") // 231
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 22; i++) //232
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove local visor updates");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 81; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //345
                        && codes[i + 13].ToString() == "call static HUDManager HUDManager::get_Instance()" //358
                        && codes[i + 81].ToString() == "ldfld UnityEngine.Animator HUDManager::batteryBlinkUI")//426
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 92; i++)//437
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove all HUDManager udpate stuff");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance() [Label55]" //479
                        && codes[i + 4].ToString() == "callvirt void HUDManager::UpdateHealthUI(int health, bool hurtPlayer)")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 5; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove HUDManager UpdateHealthUI");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //491
                        && codes[i + 1].ToString() == "call void GameNetcodeStuff.PlayerControllerB::SetHoverTipAndCurrentInteractTrigger()")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 2; i++)
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove SetHoverTipAndCurrentInteractTrigger");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") //519
                        && codes[i + 2].ToString() == "ldfld bool StartOfRound::overrideSpectateCamera")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex].operand = null;
                    codes.RemoveRange(startIndex + 1, codes.Count - (startIndex + 1));
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatchLateUpdate_ReversePatch could not remove end of method with spectate stuff");
                }

                //Plugin.Logger.LogDebug($"LateUpdate ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"LateUpdate ======================");

                return codes.AsEnumerable();
            }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("OnDisable")]
        [HarmonyReversePatch]
        public static void OnDisable_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.OnDisable_ReversePatch");

        [HarmonyPatch("NearOtherPlayers")]
        [HarmonyReversePatch]
        public static bool NearOtherPlayers_ReversePatch(object instance, PlayerControllerB playerScript, float checkRadius) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.NearOtherPlayers_ReversePatch");

        [HarmonyPatch("UpdatePlayerPositionClientRpc")]
        [HarmonyReversePatch]
        public static void UpdatePlayerPositionClientRpc_ReversePatch(object instance, Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not bypass all beginning with rpc stuff");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not change is owner with is intern and owner is local");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not change is owner with is intern and owner is local 2");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not remove all end stuff with inElevator (intern controller is never server because not spawned)");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("UpdatePlayerAnimationsToOtherClients")]
        [HarmonyReversePatch]
        public static void UpdatePlayerAnimationsToOtherClients_ReversePatch(object instance, Vector2 moveInputVector)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not use own update animation rpc method 1");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerPositionClientRpc_ReversePatch could not use own update animation rpc method 2");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("UpdatePlayerAnimationClientRpc")]
        [HarmonyReversePatch]
        public static void UpdatePlayerAnimationClientRpc_ReversePatch(object instance, int animationState, float animationSpeed)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.UpdatePlayerAnimationClientRpc_ReversePatch could not bypass rpc stuff");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("IsInSpecialAnimationClientRpc")]
        [HarmonyReversePatch]
        public static void IsInSpecialAnimationClientRpc_ReversePatch(object instance, bool specialAnimation, float timed, bool climbingLadder)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch could not bypass rpc stuff");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("SyncBodyPositionClientRpc")]
        [HarmonyReversePatch]
        public static void SyncBodyPositionClientRpc_ReversePatch(object instance, Vector3 newBodyPosition)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

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
                    Plugin.Logger.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.SyncBodyPositionClientRpc_ReversePatch could not bypass rpc stuff");
                }

                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        #endregion

        #region Postfixes

        [HarmonyPatch("PerformEmote")]
        [HarmonyPostfix]
        static void PerformEmote_PostFix(PlayerControllerB __instance)
        {
            if (__instance.playerUsername != "Player #0")
            {
                return;
            }

            InternManager.Instance.SpawnInternServerRpc(__instance.transform.position, __instance.transform.eulerAngles.y, !__instance.isInsideFactory);
        }

        [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        static void SetHoverTipAndCurrentInteractTrigger_PostFix(ref PlayerControllerB __instance,
                                                                 ref Ray ___interactRay,
                                                                 ref int ___playerMask)
        {
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
                player.ShowNameBillboard();

                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

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
                if (intern.OwnerClientId == __instance.actualClientId)
                {
                    sb.Append("Following you");
                }
                else
                {
                    sb.Append("Follow me: [E]");
                }
                __instance.cursorTip.text = sb.ToString();

                break;
            }
        }

        #endregion
    }
}
