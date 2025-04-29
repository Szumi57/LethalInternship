using LethalInternship.BehaviorTree;

namespace LethalInternship.Interns.AI.BT.ActionNodes
{
    public class TakeEntrance
    {
        public BehaviourTreeStatus Action(InternAI ai)
        {
            if (ai.ClosestEntrance == null)
            {
                Plugin.LogError("TakeEntrance Action, ClosestEntrance is null !");
                return BehaviourTreeStatus.Failure;
            }

            if (ai.ClosestEntrance.exitPoint == null)
            {
                Plugin.LogError("TakeEntrance Action, ClosestEntrance.exitPoint is null !");
                return BehaviourTreeStatus.Failure;
            }

            ai.SyncTeleportIntern(ai.ClosestEntrance.exitPoint.position, !ai.isOutside, true);
            return BehaviourTreeStatus.Success;
        }
    }
}
