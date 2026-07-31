using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class TooFarFromPos : IBTCondition
    {
        private const int STUCK_MAX = 20;
        private int stuckCounter;
        private Vector3 lastInternPos;

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (IsInternStuck(ai)) // Close enough
                return false;

            if (ai.CurrentCommand == SharedAbstractions.Enums.EnumCommandTypes.FollowPlayer)
            {
                Debug.Log($"{ai.Npc.playerUsername} FollowPlayer ?{context.PathfindingContext.GetFullPathString(context.PathController.PathIds)} {context.PathfindingContext.Destination}");
            }

            if (!context.PathController.IsCurrentPointDestination())
            {
                // Current point is not destination (here position) so : too far
                return true;
            }

            Debug.Log($"{ai.Npc.playerUsername} TooFarFromPos {context.FinalDestination}");
            Vector3 currentPoint = context.PathfindingContext.GetCurrentTargetPos(context.PathController.IndexCurrentPoint,
                                                                                  context.PathController.PathIds,
                                                                                  ai.transform.position,
                                                                                  context.FinalDestination);

            float sqrHorizontalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // Close enough from position
                stuckCounter = 0;
                Debug.Log($"{ai.Npc.playerUsername} currentPoint {currentPoint} HorizontalDistance {Mathf.Sqrt(sqrHorizontalDistance)}");
                return false;
            }

            //SharedAbstractions.Hooks.PluginLoggerHooks.PluginLoggerHook.LogDebug?.Invoke($"{context.PathController.GetCurrentPoint()} sqrHorizontalDistance {sqrHorizontalDistance}");
            if (ai.CurrentCommand == SharedAbstractions.Enums.EnumCommandTypes.FollowPlayer)
            {
                Debug.Log($"{ai.Npc.playerUsername} FollowPlayer => currentPoint {currentPoint} HorizontalDistance {Mathf.Sqrt(sqrHorizontalDistance)}");
            }


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
