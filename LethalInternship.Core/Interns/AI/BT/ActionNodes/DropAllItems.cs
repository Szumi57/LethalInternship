using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class DropAllItems : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.AreHandsFree())
            {
                PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} DropAllItems action failed, no item held ! SetCommandToFollowPlayer");
                ai.SetCommandToFollowPlayer(playVoice: false);
                return BehaviourTreeStatus.Success;
            }

            EnumOptionsGetItems options = EnumOptionsGetItems.IgnoreWeapon;
            ai.DropAllItems(options);

            return BehaviourTreeStatus.Success;
        }
    }
}
