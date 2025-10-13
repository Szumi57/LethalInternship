using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class HasItemAndInShip : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (!ai.AreHandsFree()
                && ai.NpcController.Npc.isInHangarShipRoom)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{context.InternAI.Npc.playerUsername} HasItemAndInShip true, currentCommand {context.InternAI.CurrentCommand}");
                return true;
            }
            return false;
        }
    }
}
