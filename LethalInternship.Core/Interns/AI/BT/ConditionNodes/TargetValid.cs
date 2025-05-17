namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TargetValid
    {
        public bool Condition(InternAI internAI)
        {
            if (internAI.targetPlayer == null)
            {
                return false;
            }

            if (!internAI.PlayerIsTargetable(internAI.targetPlayer, cannotBeInShip: false, overrideInsideFactoryCheck: true))
            {
                // Target is not available anymore
                return false;
            }

            // Target valid
            return true;
        }
    }
}
