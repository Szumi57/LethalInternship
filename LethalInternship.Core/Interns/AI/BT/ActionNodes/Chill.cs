using GameNetcodeStuff;
using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class Chill : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            
            // Set where the intern should look
            SetInternLookAt(ai);

            // Chill
            ai.StopMoving();

            // Try play voice
            TryPlayCurrentStateVoiceAudio(ai);

            // Crouch
            ai.FollowCrouchIfCanDo();

            // Emotes
            ai.NpcController.MimicEmotes(ai.targetPlayer);

            return BehaviourTreeStatus.Success;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            EnumVoicesState voiceState = ai.CurrentCommand == EnumCommandTypes.FollowPlayer ? EnumVoicesState.Chilling : EnumVoicesState.Waiting;

            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = voiceState,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        private void SetInternLookAt(InternAI ai, Vector3? position = null)
        {
            if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
            {
                LookAtWhatPlayerPointingAt(ai);
            }
            else
            {
                if (position.HasValue)
                {
                    ai.NpcController.OrderToLookAtPlayer(position.Value + new Vector3(0, 2.35f, 0));
                }
                else
                {
                    // Looking at player or forward
                    PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    if (playerToLook != null)
                    {
                        ai.NpcController.OrderToLookAtPlayer(playerToLook.playerEye.position);
                    }
                    else
                    {
                        ai.NpcController.OrderToLookForward();
                    }
                }
            }
        }

        private void LookAtWhatPlayerPointingAt(InternAI ai)
        {
            // Look where the target player is looking
            Ray interactRay = new Ray(ai.targetPlayer.gameplayCamera.transform.position, ai.targetPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                ai.NpcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                ai.NpcController.OrderToLookForward();
            }
            else
            {
                // Check if looking at a player/intern
                foreach (var hit in raycastHits)
                {
                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null
                        && player.playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        ai.NpcController.OrderToLookAtPosition(hit.point);
                        ai.NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        ai.NpcController.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                        ai.NpcController.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    ai.NpcController.OrderToLookAtPosition(hit.point);
                    ai.NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }
    }
}
