using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromEnemy : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                return false;
            }

            Vector3 enemyPos = context.CurrentEnemy.transform.position;
            float grabDist = ai.NpcController.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale * 1.5f;
            float sqrHorizontalDistance = Vector3.Scale(enemyPos - ai.transform.position, new Vector3(1f, 0, 1f)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(enemyPos - (ai.transform.position + new Vector3(0, 1.7f, 0)), new Vector3(0, 1f, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < grabDist * grabDist
                && sqrVerticalDistance < grabDist * grabDist)
            {
                return false;
            }
            
            return true;
        }
    }
}
