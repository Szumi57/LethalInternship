namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class HasItemAndInShip
    {
        public bool Condition(InternAI ai)
        {
            if (!ai.AreHandsFree()
                && ai.NpcController.Npc.isInHangarShipRoom)
            {
                return true;
            }
            return false;
        }
    }
}
