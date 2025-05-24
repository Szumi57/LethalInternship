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
                return true;
            }
            return false;
        }
    }
}
