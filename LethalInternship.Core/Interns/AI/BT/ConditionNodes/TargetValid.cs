using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TargetValid : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.targetPlayer == null)
            {
                return false;
            }

            if (!ai.PlayerIsTargetable(ai.targetPlayer, cannotBeInShip: false, overrideInsideFactoryCheck: true))
            {
                // Target is not available anymore
                return false;
            }

            // Target valid
            context.PathController.SetNewDestination(new DJKMovingPoint(ai.targetPlayer.transform, $"targetPlayer {ai.targetPlayer.playerUsername}"));
            return true;
        }
    }
}
