using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromObject : IBTCondition
    {
        private const int STUCK_MAX = 25;
        private int stuckCounter;
        private Vector3 lastInternPos;

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (IsInternStuck(ai)) // Close enough
                return false;

            if (context.TargetItem == null)
            {
                PluginLoggerHook.LogError?.Invoke("TooFarFromObject action, targetItem is null");
                return false;
            }

            float grabDist = ai.NpcController.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale * 1.5f;
            float sqrHorizontalDistance = Vector3.Scale(context.TargetItem.transform.position - ai.transform.position, new Vector3(1f, 0, 1f)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(context.TargetItem.transform.position - (ai.transform.position + new Vector3(0, 1.7f, 0)), new Vector3(0, 1f, 0)).sqrMagnitude;
            // Close enough to item for grabbing
            //if (sqrHorizontalDistance < (grabDist * grabDist) + 4f)
            //{
            //    PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} TooFarFromObject {context.TargetItem} hor {sqrHorizontalDistance} <? {grabDist * grabDist}");
            //}
            if (sqrHorizontalDistance < grabDist * grabDist
                && sqrVerticalDistance < grabDist * grabDist)
            {
                // Close enough from object
                stuckCounter = 0;
                return false;
            }

            // Too far from object
            return true;
        }

        private bool IsInternStuck(InternAI ai)
        {
            // Stuck ?
            // -------
            if ((lastInternPos - ai.Npc.transform.position).sqrMagnitude < 0.1f * 0.1f)
            {
                stuckCounter++;
                //PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} stuckCounter {stuckCounter} {(lastInternPos - ai.Npc.transform.position).magnitude}");
            }
            else // Not stuck
                stuckCounter = 0;

            lastInternPos = ai.Npc.transform.position;

            if (stuckCounter > STUCK_MAX)
            {
                PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} TooFarFromObject stuck, bypass distance check");
                stuckCounter = 0;
                // Close enough from position
                return true;
            }

            return false;
        }
    }
}
