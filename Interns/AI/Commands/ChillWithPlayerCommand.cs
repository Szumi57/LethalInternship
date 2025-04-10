using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI.Commands
{
    public class ChillWithPlayerCommand : ICommandAI
    {
        private readonly InternAI ai;

        private NpcController Controller { get { return ai.NpcController; } }

        /// <summary>
        /// Represents the distance between the body of intern (<c>PlayerControllerB</c> position) and the target player (owner of intern), 
        /// only on axis x and z, y at 0, and squared
        /// </summary>
        private float SqrHorizontalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale(ai.targetPlayer.transform.position - Controller.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        /// <summary>
        /// Represents the distance between the body of intern (<c>PlayerControllerB</c> position) and the target player (owner of intern), 
        /// only on axis y, x and z at 0, and squared
        /// </summary>
        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale(ai.targetPlayer.transform.position - Controller.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            }
        }

        public ChillWithPlayerCommand(InternAI internAI)
        {
            ai = internAI;
        }

        public EnumCommandEnd Execute()
        {
            // Update target last known position
            PlayerControllerB? playerTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (playerTarget != null)
            {
                ai.TargetLastKnownPosition = ai.targetPlayer.transform.position;
            }

            // Target too far, get close to him
            // note: not the same distance to compare in horizontal or vertical distance
            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                || SqrVerticalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                Controller.OrderToLookForward();
                    Plugin.LogDebug("chill add follow");
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return EnumCommandEnd.Finished;
            }

            // Set where the intern should look
            SetInternLookAt();

            // Chill
            ai.StopMoving();

            // Emotes
            Controller.MimicEmotes(ai.targetPlayer);

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return EnumCommandEnd.Finished;
        }

        private void SetInternLookAt(Vector3? position = null)
        {
            if (Plugin.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
            {
                LookAtWhatPlayerPointingAt();
            }
            else
            {
                if (position.HasValue)
                {
                    Controller.OrderToLookAtPlayer(position.Value + new Vector3(0, 2.35f, 0));
                }
                else
                {
                    // Looking at player or forward
                    PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    if (playerToLook != null)
                    {
                        Controller.OrderToLookAtPlayer(playerToLook.playerEye.position);
                    }
                    else
                    {
                        Controller.OrderToLookForward();
                    }
                }
            }
        }

        private void LookAtWhatPlayerPointingAt()
        {
            // Look where the target player is looking
            Ray interactRay = new Ray(ai.targetPlayer.gameplayCamera.transform.position, ai.targetPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                Controller.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                Controller.OrderToLookForward();
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
                        Controller.OrderToLookAtPosition(hit.point);
                        Controller.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        Controller.SetTurnBodyTowardsDirection(ai.targetPlayer.gameplayCamera.transform.forward);
                        Controller.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    Controller.OrderToLookAtPosition(hit.point);
                    Controller.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }


        public void PlayerHeard(Vector3 noisePosition)
        {
            // Look at origin of sound
            SetInternLookAt(noisePosition);
        }

        public string GetBillboardStateIndicator()
        {
            return string.Empty;
        }

        private void TryPlayCurrentStateVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.Chilling,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }
    }
}
