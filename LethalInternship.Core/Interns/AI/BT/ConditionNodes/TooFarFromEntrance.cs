using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromEntrance
    {
        public bool Condition(InternAI ai)
        {
            if (ai.ClosestEntrance == null)
            {
                PluginLoggerHook.LogError?.Invoke("TooFarFromEntrance Condition, ClosestPosOfEntrance is null !");
                return false;
            }

            if ((ai.NpcController.Npc.transform.position - ai.ClosestEntrance.entrancePoint.position).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE)
            {
                return false;
            }

            return true;
        }
    }
}
