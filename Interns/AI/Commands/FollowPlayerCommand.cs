using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI.Commands
{
    public class FollowPlayerCommand : ICommandAI
    {
        private readonly InternAI ai;
        private NpcController Controller { get { return ai.NpcController; } }

        public FollowPlayerCommand(InternAI internAI)
        {
            ai = internAI;
        }

        public EnumCommandEnd Execute()
        {
            // Lost target player
            if (ai.targetPlayer == null)
            {
                // Last position unknown
                if (ai.TargetLastKnownPosition.HasValue)
                {
                    ai.QueueNewCommand(new LostPlayerCommand(ai));
                    return EnumCommandEnd.Finished;
                }

                ai.QueueNewCommand(new LookingForPlayer(ai));
                return EnumCommandEnd.Finished;
            }

            if (!ai.PlayerIsTargetable(ai.targetPlayer, false, true))
            {
                // Target is not available anymore
                ai.QueueNewCommand(new LookingForPlayer(ai));
                return EnumCommandEnd.Finished;
            }

            // Target is in awarness range
            float sqrHorizontalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - Controller.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistanceWithTarget = Vector3.Scale(ai.targetPlayer.transform.position - Controller.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR
                    && sqrVerticalDistanceWithTarget < Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER)
            {
                ai.TargetLastKnownPosition = ai.targetPlayer.transform.position;
                ai.SetDestinationToPositionInternAI(ai.targetPlayer.transform.position);
            }
            else
            {
                // Target outside of awareness range, if ai does not see target, then the target is lost
                //Plugin.LogDebug($"{ai.NpcController.Npc.playerUsername} no see target, still in range ? too far {sqrHorizontalDistanceWithTarget > Const.DISTANCE_AWARENESS_HOR * Const.DISTANCE_AWARENESS_HOR}, too high/low {sqrVerticalDistanceWithTarget > Const.DISTANCE_AWARENESS_VER * Const.DISTANCE_AWARENESS_VER}");
                PlayerControllerB? checkTarget = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (checkTarget == null)
                {
                    ai.QueueNewCommand(new LostPlayerCommand(ai));
                    return EnumCommandEnd.Finished;
                }
                else
                {
                    // Target still visible
                    ai.TargetLastKnownPosition = ai.targetPlayer.transform.position;
                    ai.SetDestinationToPositionInternAI(ai.targetPlayer.transform.position);

                    // Bring closer with teleport if possible
                    ai.CheckAndBringCloserTeleportIntern(0.8f);
                }
            }

            // Follow player
            // If close enough, chill with player
            // Sprint if far, stop sprinting if close
            if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistanceWithTarget < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                ai.QueueNewCommand(new ChillWithPlayerCommand(ai));
                return EnumCommandEnd.Finished;
            }
            else if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                     || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
            {
                Controller.OrderToSprint();
            }
            else if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
            {
                Controller.OrderToStopSprint();
            }

            ai.OrderAgentAndBodyMoveToDestination();

            // Try play voice
            TryPlayCurrentStateVoiceAudio();

            ai.QueueNewCommand(this);
            return EnumCommandEnd.Finished;
        }

        public void PlayerHeard(Vector3 noisePosition) { }

        private void TryPlayCurrentStateVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FollowingPlayer,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        public string GetBillboardStateIndicator()
        {
            return string.Empty;
        }
    }
}
