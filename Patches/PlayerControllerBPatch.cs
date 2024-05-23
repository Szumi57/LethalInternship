using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Utils;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LethalInternship.Patches
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
                                  bool ___disabledJetpackControlsThisFrame,
                                  float ___updatePlayerAnimationsInterval,
                                  float ___currentAnimationSpeed,
                                  float ___previousAnimationSpeed,
                                  ref List<int> ___currentAnimationStateHash,
                                  ref List<int> ___previousAnimationStateHash)
        {
            InternAI? internAI = InternManager.GetInternAI((int)__instance.playerClientId);
            if (internAI?.NpcController.Npc.playerClientId != __instance.playerClientId)
            {
                return true;
            }
            // Use Intern update and pass all needed paramaters back and forth
            internAI.NpcController.IsCameraDisabled = ___isCameraDisabled;
            internAI.NpcController.IsJumping = ___isJumping;
            internAI.NpcController.CrouchMeter = ___crouchMeter;
            internAI.NpcController.IsWalking = ___isWalking;
            internAI.NpcController.DisabledJetpackControlsThisFrame = ___disabledJetpackControlsThisFrame;
            internAI.NpcController.UpdatePlayerAnimationsInterval = ___updatePlayerAnimationsInterval;
            internAI.NpcController.CurrentAnimationSpeed = ___currentAnimationSpeed;
            internAI.NpcController.PreviousAnimationSpeed = ___previousAnimationSpeed;
            internAI.NpcController.CurrentAnimationStateHash = ___currentAnimationStateHash;
            internAI.NpcController.PreviousAnimationStateHash = ___previousAnimationStateHash;

            internAI.NpcController.Update();

            ___isCameraDisabled = internAI.NpcController.IsCameraDisabled;
            ___crouchMeter = internAI.NpcController.CrouchMeter;
            ___isWalking = internAI.NpcController.IsWalking;
            ___currentAnimationStateHash = internAI.NpcController.CurrentAnimationStateHash;
            ___previousAnimationStateHash = internAI.NpcController.PreviousAnimationStateHash;

            return false;
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_PreFix(PlayerControllerB __instance)
        {
            InternAI? internAI = InternManager.GetInternAI((int)__instance.playerClientId);
            if (internAI?.NpcController.Npc.playerClientId == __instance.playerClientId)
            {
                LateUpdate_ReversePatch(internAI.NpcController.Npc);
                return false;
            }
            return true;
        }

        //[HarmonyPatch("Awake")]
        //[HarmonyPrefix]
        //static bool Awake_PreFix(PlayerControllerB __instance,
        //                         ref bool ___isCameraDisabled,
        //                         ref Vector3 ___rightArmProceduralTargetBasePosition,
        //                         ref int ___previousAnimationState)
        //{
        //    Plugin.Logger.LogDebug($"wtf {(int)__instance.playerClientId}");
        //    InternAI? internAI = StartOfRoundPatch.GetInternAI((int)__instance.playerClientId);
        //    if (internAI?.NpcController.Npc.playerClientId == __instance.playerClientId)
        //    {
        //        internAI.NpcController.IsCameraDisabled = ___isCameraDisabled;
        //        internAI.NpcController.RightArmProceduralTargetBasePosition = ___rightArmProceduralTargetBasePosition;
        //        internAI.NpcController.PreviousAnimationState = ___previousAnimationState;

        //        internAI.NpcController.Awake();

        //        ___isCameraDisabled = internAI.NpcController.IsCameraDisabled;
        //        ___rightArmProceduralTargetBasePosition = internAI.NpcController.RightArmProceduralTargetBasePosition;
        //        ___previousAnimationState = internAI.NpcController.PreviousAnimationState;
        //        return false;
        //    }
        //    return true;
        //}

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        static bool DamagePlayer_PreFix(PlayerControllerB __instance)
        {
            if (!PatchesUtil.IsPlayerIntern(__instance))
            {
                // todo: Bootleg invulnerability
                //Plugin.Logger.LogDebug($"Bootleg invulnerability (return false)");
                return false;
                return true;
            }
            return true;
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayer_PreFix(PlayerControllerB __instance)
        {
            if (!PatchesUtil.IsPlayerIntern(__instance))
            {
                // todo: Bootleg invincibility
                //Plugin.Logger.LogDebug($"Bootleg invincibility (return false)");
                return false;
                return true;
            }
            return true;
        }

        #endregion

        #region Transpilers

        [HarmonyPatch("DamagePlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DamagePlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // remove all HUDelement udpates and all that stuff if player is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance() [Label7]"
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::ShakeCamera.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::UpdateHealthUI.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DamagePlayer_Transpiler could not insert instruction if is intern for HUDManager::UIAudio and WalkieTalkie.TransmitOneShotAudio.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DamagePlayer_Transpiler could not insert instruction if is intern for AudioSource::PlayOneShot and StartOfRound::LocalPlayerDamagedEvent.");
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

        [HarmonyPatch("KillPlayer")]
        [HarmonyTranspiler]
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayer_Transpiler could not insert instruction if is intern for StartOfRound::get_Instance() and HUDManager::get_Instance().");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayer_Transpiler could not insert instruction if is intern for FindObjectOfType and others.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayer_Transpiler could not insert instruction if is intern for SwitchCamera and others.");
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

        [HarmonyPatch("KillPlayerServerRpc")]
        [HarmonyTranspiler]
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayerServerRpc_Transpiler could not insert instruction if is intern for livingPlayers--.");
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

        [HarmonyPatch("KillPlayerClientRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> KillPlayerClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].ToString() == "ldstr \"A player died. player object: \"") //76
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].operand = "An intern died. Intern object: ";
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayerClientRpc_Transpiler could not change string \"A player died. player object: \".");
            }

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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayerClientRpc_Transpiler could not insert instruction if is intern for livingPlayers-- after isServer.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayerClientRpc_Transpiler could not insert instruction if is intern for playerVoicePitchTargets, playerVoicePitchLerpSpeed.");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.KillPlayerClientRpc_Transpiler could not insert instruction if is intern for UpdateBoxesSpectateUI(),  UpdatePlayerVoiceEffects().");
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

        [HarmonyPatch("GrabObjectServerRpc")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GrabObjectServerRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldloc.1 NULL" //110
                    && codes[i + 1].ToString() == "ldarg.0 NULL"
                    && codes[i + 2].ToString() == "ldfld ulong GameNetcodeStuff.PlayerControllerB::actualClientId"
                    && codes[i + 3].ToString() == "callvirt void Unity.Netcode.NetworkObject::ChangeOwnership(ulong newOwnerClientId)")
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

                PatchesUtil.InsertIsPlayerInternInstructions(codes, generator, startIndex, 4);


                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.GrabObjectServerRpc_Transpiler could not remove change ownership");
            }


            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"GrabObjectServerRpc ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"GrabObjectServerRpc ======================");
            return codes.AsEnumerable();
        }

        [HarmonyPatch("SwitchToItemSlot")]
        [HarmonyTranspiler]
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.SwitchToItemSlot_Transpiler could not remove hudmanager itemsloticonframes");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.SwitchToItemSlot_Transpiler could not remove hudmanager set_sprite");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.SwitchToItemSlot_Transpiler could not remove hudmanager clearcontroltips");
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.SwitchToItemSlot_Transpiler could not remove hudmanager holdingTwoHandedItem");
            }

            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"SwitchToItemSlot ======================");

            return codes.AsEnumerable();
        }

        [HarmonyPatch("DiscardHeldObject")]
        [HarmonyTranspiler]
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DiscardHeldObject_Transpiler could not remove HUDManager::itemSlotIcons");
            }

            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DiscardHeldObject ======================");

            return codes.AsEnumerable();
        }

        [HarmonyPatch("DropAllHeldItems")]
        [HarmonyTranspiler]
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
                Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.DropAllHeldItems_Transpiler could not remove HUDManager::holdingTwoHandedItem, itemSlotIcons, ClearControlTips");
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
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //0
                        && codes[i + 1].ToString() == "ldfld QuickMenuManager GameNetcodeStuff.PlayerControllerB::quickMenuManager"
                        && codes[i + 2].ToString() == "ldfld bool QuickMenuManager::isMenuOpen"
                        && codes[i + 3].ToString() == "brfalse Label1"
                        && codes[i + 4].ToString() == "ret NULL")
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove isMenuOpen condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //11
                        && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsServer()"
                        && codes[i + 2].ToString() == "brfalse Label4"
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
                    codes[startIndex].operand = codes[startIndex + 10].labels[0];
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove isHostPlayerObject condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 3; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL [Label7]" //0
                        && codes[i + 1].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::isTypingChat"
                        && codes[i + 2].ToString() == "brfalse Label8"
                        && codes[i + 3].ToString() == "ret NULL")
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.JumpPerformed_ReversePatch could not remove isTypingChat condition");
                }

                //Plugin.Logger.LogDebug($"Jump_performed ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"Jump_performed ======================");
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [HarmonyPatch("CalculateGroundNormal")]
        [HarmonyReversePatch]
        public static void CalculateGroundNormal_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.PlayerControllerBPatch.CalculateGroundNormal_ReversePatch");

        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyReversePatch]
        public static void PlayerHitGroundEffects_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch");

        [HarmonyPatch("UpdatePlayerAnimationsToOtherClients")]
        [HarmonyReversePatch]
        public static void UpdatePlayerAnimationsToOtherClients_ReversePatch(object instance, Vector2 moveInputVector) => throw new NotImplementedException("Stub LethalInternship.Patches.PlayerControllerBPatch.UpdatePlayerAnimationsToOtherClients_ReversePatch");

        [HarmonyPatch("CheckConditionsForEmote")]
        [HarmonyReversePatch]
        public static bool CheckConditionsForEmote_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.PlayerControllerBPatch.CheckConditionsForEmote_ReversePatch");

        [HarmonyPatch("BeginGrabObject")]
        [HarmonyReversePatch]
        public static void BeginGrabObject_ReversePatch(object instance, GrabbableObject grabbableObject)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                //Plugin.Logger.LogDebug($"BeginGrabObject ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //35
                        && codes[i + 1].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::twoHanded"
                        && codes[i + 4].ToString() == "ldfld float GameNetcodeStuff.PlayerControllerB::sinkingValue")
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.BeginGrabObject_ReversePatch could not remove hitray condition");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 5; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //44
                        && codes[i + 1].ToString() == "ldflda UnityEngine.RaycastHit GameNetcodeStuff.PlayerControllerB::hit"
                        && codes[i + 2].ToString() == "call UnityEngine.Collider UnityEngine.RaycastHit::get_collider()"
                        && codes[i + 3].ToString() == "callvirt UnityEngine.Transform UnityEngine.Component::get_transform()"
                        && codes[i + 4].ToString() == "callvirt UnityEngine.GameObject UnityEngine.Component::get_gameObject()"
                        && codes[i + 5].ToString() == "callvirt GrabbableObject UnityEngine.GameObject::GetComponent()")
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
                    codes[startIndex + 5].opcode = OpCodes.Ldarg_1;
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.BeginGrabObject_ReversePatch could not force to take grabbable object");
                }

                //Plugin.Logger.LogDebug($"BeginGrabObject ======================");
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
                //}
                //Plugin.Logger.LogDebug($"BeginGrabObject ======================");
                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

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
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //19
                        && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsServer()"
                        && codes[i + 2].ToString() == "brfalse Label5"
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
                        if (i == startIndex + 2)
                        {
                            codes[i].opcode = OpCodes.Br;// use label5 here to bypass condition check
                        }
                        else
                        {
                            codes[i].opcode = OpCodes.Nop;
                            codes[i].operand = null;
                        }
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove isHostPlayerObject condition 1");
                }

                // ----------------------------------------------------------------------
                // invalid IL code op ?????
                //for (var i = 0; i < codes.Count - 29; i++)
                //{
                //    if (codes[i].ToString() == "ldarg.0 NULL" //59
                //        && codes[i + 1].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::isTestingPlayer"
                //        && codes[i + 29].ToString() == "ldfld UnityEngine.Transform GameNetcodeStuff.PlayerControllerB::usernameBillboard") // 88
                //    {
                //        startIndex = i;
                //        break;
                //    }
                //}
                //if (startIndex > -1)
                //{
                //    for (var i = startIndex; i < startIndex + 45; i++) //104
                //    {
                //        codes[i].opcode = OpCodes.Nop;
                //        codes[i].operand = null;
                //    }
                //    startIndex = -1;
                //}
                //else
                //{
                //    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove billboard and canvas");
                //}

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 7; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL" //107
                        && codes[i + 1].ToString() == "call bool Unity.Netcode.NetworkBehaviour::get_IsServer()"
                        && codes[i + 2].ToString() == "brfalse Label19"
                        && codes[i + 3].ToString() == "ldarg.0 NULL"
                        && codes[i + 4].ToString() == "ldfld bool GameNetcodeStuff.PlayerControllerB::isHostPlayerObject"
                        && codes[i + 7].ToString() == "call void GameNetcodeStuff.PlayerControllerB::PlayerLookInput()")
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    for (var i = startIndex; i < startIndex + 8; i++)
                    {
                        if (i == startIndex + 2)
                        {
                            codes[i].opcode = OpCodes.Br;// use label9 here to bypass condition check
                        }
                        else
                        {
                            codes[i].opcode = OpCodes.Nop;
                            codes[i].operand = null;
                        }
                    }
                    startIndex = -1;
                }
                else
                {
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove isHostPlayerObject condition 2 and lookinput");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 21; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL [Label23, Label31, Label32, Label33]" //210
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove local visor updates");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove all HUDManager udpate stuff");
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove HUDManager UpdateHealthUI");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL [Label53, Label56]" //491
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove SetHoverTipAndCurrentInteractTrigger");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].ToString() == "ldarg.0 NULL [Label57, Label58]" //519
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
                    Plugin.Logger.LogError($"LethalInternship.Patches.PlayerControllerBPatch.LateUpdate_ReversePatch could not remove end of method with spectate stuff");
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

        [HarmonyPatch("FirstEmptyItemSlot")]
        [HarmonyReversePatch]
        public static int FirstEmptyItemSlot_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch");

        #endregion

        #region

        [HarmonyPatch("PerformEmote")]
        [HarmonyPostfix]
        static void PerformEmote_PostFix(PlayerControllerB __instance)
        {
            if (__instance.playerUsername != "Player #0")
            {
                return;
            }

            InternManager.SpawnIntern(__instance.transform, !__instance.isInsideFactory);
        }

        #endregion
    }

    // TODO todo grabbable object anim
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_PreFix(ref GrabbableObject __instance)
        {
            return true;
            //if (__instance.parentObject != null)
            //{
            //    __instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("Player")).transform.localScale = Vector3.one;
            //    Plugin.Logger.LogDebug($"prefix rot {__instance.parentObject.rotation}, {__instance.itemProperties.rotationOffset}");
            //    Plugin.Logger.LogDebug($"prefix pos {__instance.parentObject.position}, {__instance.itemProperties.positionOffset}");
            //    Plugin.Logger.LogDebug($"prefix {__instance.parentObject.gameObject.name}, {__instance.parentObject.parent?.gameObject.name}, {__instance.parentObject.parent?.parent?.gameObject.name}");

            //    __instance.transform.rotation = __instance.parentObject.rotation;
            //    __instance.transform.Rotate(__instance.itemProperties.rotationOffset);
            //    __instance.transform.position = __instance.parentObject.position;
            //    Vector3 vector = __instance.itemProperties.positionOffset;
            //    vector = __instance.parentObject.rotation * vector;
            //    __instance.transform.position += vector;

            //    var handpos = __instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("hand.R")).transform.position;
            //    Plugin.Logger.LogDebug($"dist holder/hand {(__instance.parentObject.position - handpos).magnitude}");
            //    Plugin.Logger.LogDebug($"dist object/hand {(__instance.transform.position - handpos).magnitude}");
            //}
            //__instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("hand.R")).transform;

            //return false;
            //if (__instance.parentObject != null)
            //{

            //Plugin.Logger.LogDebug($"{__instance.itemProperties.positionOffset}, {__instance.itemProperties.positionOffset}");
            //}
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        static void LateUpdate_PostFix(ref GrabbableObject __instance)
        {
            if (__instance.parentObject != null)
            {

                //__instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("Player")).transform.localScale *= 0.85f;
                //Plugin.Logger.LogDebug($"prefix rot {__instance.parentObject.rotation}, {__instance.itemProperties.rotationOffset}");
                //Plugin.Logger.LogDebug($"prefix pos {__instance.parentObject.position}, {__instance.itemProperties.positionOffset}");
                //Plugin.Logger.LogDebug($"prefix {__instance.parentObject.gameObject.name}, {__instance.parentObject.parent?.gameObject.name}, {__instance.parentObject.parent?.parent?.gameObject.name}");
            }
            //if (__instance.parentObject != null)
            //{

            //Plugin.Logger.LogDebug($"{__instance.itemProperties.positionOffset}, {__instance.itemProperties.positionOffset}");
            //}
        }
    }
}
