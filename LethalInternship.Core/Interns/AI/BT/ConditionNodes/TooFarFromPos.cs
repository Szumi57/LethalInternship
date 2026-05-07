using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromPos : IBTCondition
    {
        private const int STUCK_MAX = 25;
        private int stuckCounter;
        private Vector3 lastInternPos;

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (IsInternStuck(ai)) // Close enough
                return false;

            if (!context.PathController.IsCurrentPointDestination())
            {
                // Current point is not destination (here position) so : too far
                return true;
            }

            Vector3 currentPoint = context.PathController.GetCurrentPointPos(ai.transform.position);

            float sqrHorizontalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // Close enough from position
                stuckCounter = 0;
                return false;
            }

            //SharedAbstractions.Hooks.PluginLoggerHooks.PluginLoggerHook.LogDebug?.Invoke($"{context.PathController.GetCurrentPoint()} sqrHorizontalDistance {sqrHorizontalDistance}");
            // Too far from position
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
                PluginLoggerHook.LogDebug?.Invoke($"-- {ai.Npc.playerUsername} TooFarFromPos stuck, bypass distance check");
                stuckCounter = 0;
                // Close enough from position
                return true;
            }

            return false;
        }
    }
}
