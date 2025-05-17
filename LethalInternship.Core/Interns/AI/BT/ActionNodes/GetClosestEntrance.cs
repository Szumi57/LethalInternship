using LethalInternship.Core.BehaviorTree;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GetClosestEntrance
    {
        public BehaviourTreeStatus Action(InternAI ai, EntranceTeleport[] entrancesTeleportArray)
        {
            Vector3 entityPos = ai.NpcController.Npc.transform.position;

            ai.ClosestEntrance = GetClosest(entityPos, entrancesTeleportArray);
            return BehaviourTreeStatus.Success;
        }

        private EntranceTeleport? GetClosest(Vector3 entityPos, EntranceTeleport[] entrancesTeleportArray)
        {
            if (entrancesTeleportArray == null
                || entrancesTeleportArray.Length == 0)
            {
                return null;
            }

            if (entrancesTeleportArray.Length == 1)
            {
                return entrancesTeleportArray[0];
            }

            EntranceTeleport? entrance = entrancesTeleportArray[0];
            float minDistance = (entityPos - entrancesTeleportArray[0].entrancePoint.position).sqrMagnitude;
            float currentDist;
            for (int i = 1; i < entrancesTeleportArray.Length; i++)
            {
                currentDist = (entityPos - entrancesTeleportArray[i].entrancePoint.position).sqrMagnitude;
                if (currentDist < minDistance)
                {
                    minDistance = currentDist;
                    entrance = entrancesTeleportArray[i];
                }
            }
            return entrance;
        }
    }
}
