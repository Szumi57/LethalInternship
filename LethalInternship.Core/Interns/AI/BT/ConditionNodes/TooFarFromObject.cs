using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromObject : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("TooFarFromObject action, targetItem is null");
                return false;
            }

            float grabDist = ai.NpcController.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale * 1.5f;
            float sqrHorizontalDistance = Vector3.Scale(context.TargetItem.transform.position - ai.transform.position, new Vector3(1f, 0, 1f)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(context.TargetItem.transform.position - (ai.transform.position + new Vector3(0, 1.7f, 0)), new Vector3(0, 1f, 0)).sqrMagnitude;
            // Close enough to item for grabbing
            //if (sqrHorizontalDistance < (grabDist * grabDist) + 1f)
            //{
            //    PluginLoggerHook.LogDebug?.Invoke($"TooFarFromObject {context.TargetItem} hor {sqrHorizontalDistance} <? {grabDist * grabDist}");
            //}
            if (sqrHorizontalDistance < grabDist * grabDist
                && sqrVerticalDistance < grabDist * grabDist)
            {
                return false;
            }

            return true;
        }
    }
}
