namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromObject
    {
        public bool Condition(InternAI ai, GrabbableObject? targetItem)
        {
            if (targetItem == null)
            {
                Plugin.LogError("targetItem is null");
                return false;
            }

            float sqrMagDistanceItem = (targetItem.transform.position - ai.NpcController.Npc.transform.position).sqrMagnitude;
            // Close enough to item for grabbing
            if (sqrMagDistanceItem < ai.NpcController.Npc.grabDistance * ai.NpcController.Npc.grabDistance * Plugin.Config.InternSizeScale.Value)
            {
                return false;
            }

            return true;
        }
    }
}
