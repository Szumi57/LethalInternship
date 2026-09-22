using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TargetValid : IBTCondition
    {
        private DJKMovingPoint _targetMovingPoint = new DJKMovingPoint();

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
            _targetMovingPoint.Transform = ai.targetPlayer.transform;
            _targetMovingPoint.Name = $"targetPlayer {ai.targetPlayer.playerUsername}";
            context.PathfindingContext.SetDestination(_targetMovingPoint.Clone(InternManager.Instance.Pools));
            return true;
        }
    }
}
