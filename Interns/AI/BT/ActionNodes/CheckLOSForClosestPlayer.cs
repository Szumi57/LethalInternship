using GameNetcodeStuff;
using LethalInternship.BehaviorTree;
using LethalInternship.Constants;
using LethalInternship.Enums;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class CheckLOSForClosestPlayer
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            // Try to find the closest player to target
            PlayerControllerB? player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player == null)
            {
                return BehaviourTreeStatus.Failure;
            }

            // Play voice
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.LostAndFound,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });

            // Assign to new target
            ai.SyncAssignTargetAndSetMovingTo(player);
            if (Plugin.Config.ChangeSuitAutoBehaviour.Value)
            {
                ai.ChangeSuitInternServerRpc(ai.NpcController.Npc.playerClientId, player.currentSuitID);
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
