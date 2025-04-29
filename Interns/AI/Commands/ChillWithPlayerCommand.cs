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

        public void Execute()
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
                return;
            }

            // Set where the intern should look
            ai.SetInternLookAt();

            // Chill
            ai.StopMoving();

            // Emotes
            Controller.MimicEmotes(ai.targetPlayer);

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return;
        }

        public void PlayerHeard(Vector3 noisePosition)
        {
            // Look at origin of sound
            ai.SetInternLookAt(noisePosition);
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

        public EnumCommandTypes GetCommandType()
        {
            return EnumCommandTypes.None;
        }
    }
}
