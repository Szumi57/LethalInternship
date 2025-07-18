using GameNetcodeStuff;
using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckLOSForClosestPlayer : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

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
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });

            // Assign to new target
            ai.SyncAssignTargetAndSetMovingTo(player);
            if (PluginRuntimeProvider.Context.Config.ChangeSuitAutoBehaviour)
            {
                ai.ChangeSuitInternServerRpc(ai.NpcController.Npc.playerClientId, player.currentSuitID);
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
