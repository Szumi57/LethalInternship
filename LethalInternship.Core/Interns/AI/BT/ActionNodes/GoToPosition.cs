using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class GoToPosition : IBTAction
    {
        public bool CanRun = true; // const for now

        private const float MIN_DISTANCE_HOR = 1f;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Check if we should take entrance
            DJKEntrancePoint? entrancePoint = context.PathController.GetCurrentPoint() as DJKEntrancePoint;
            if (entrancePoint != null)
            {
                // Take entrance
                if (TakeEntrance(ai, entrancePoint))
                {
                    context.PathController.SetToNextPoint();
                }
            }

            Vector3 currentPoint = context.PathController.GetCurrentPointPos(ai.transform.position);
            //SharedAbstractions.Hooks.PluginLoggerHooks.PluginLoggerHook.LogDebug?.Invoke($"\"{ai.Npc.playerUsername}\" {ai.Npc.playerClientId} => {context.PathController} {currentPoint}");

            // todo remove
            DJKItemPoint? vPoint = context.PathController.GetCurrentPoint() as DJKItemPoint;
            if (vPoint != null)
            {
                DrawUtil.DrawLine(ai.LineRendererUtil.GetLineRenderer(), currentPoint, currentPoint + new Vector3(0, 5f, 0), Color.magenta);
            }

            // Go to position
            MoveToPosition(ai, currentPoint);

            // Check for to distance to current point
            if (CloseEnoughOfCurrentPoint(ai, currentPoint))
            {
                context.PathController.SetToNextPoint();
            }
            return BehaviourTreeStatus.Success;
        }

        private void MoveToPosition(InternAI ai, Vector3 currentPoint)
        {
            if (CanRun)
            {
                float sqrHorizontalDistanceWithTarget = Vector3.Scale(currentPoint - ai.NpcController.Npc.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
                float sqrVerticalDistanceWithTarget = Vector3.Scale(currentPoint - ai.NpcController.Npc.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;

                if (sqrHorizontalDistanceWithTarget > Const.DISTANCE_START_RUNNING * Const.DISTANCE_START_RUNNING
                     || sqrVerticalDistanceWithTarget > 0.3f * 0.3f)
                {
                    ai.NpcController.OrderToSprint();
                }
                else //if (sqrHorizontalDistanceWithTarget < Const.DISTANCE_STOP_RUNNING * Const.DISTANCE_STOP_RUNNING)
                {
                    ai.NpcController.OrderToStopSprint();
                }
            }

            ai.NpcController.OrderToLookForward();

            ai.SetDestinationToPositionInternAI(currentPoint);
            ai.OrderAgentAndBodyMoveToDestination();

            if (ai.CurrentCommand == EnumCommandTypes.FollowPlayer)
            {
                ai.FollowCrouchIfCanDo();
                TryPlayCurrentStateVoiceAudio(ai);
            }
        }

        private bool TakeEntrance(InternAI ai, DJKEntrancePoint entrance)
        {
            Vector3 entrancePoint = entrance.GetClosestPointTo(ai.transform.position);

            // Close enough to entrance
            if ((ai.transform.position - entrancePoint).sqrMagnitude < Const.DISTANCE_TO_ENTRANCE * Const.DISTANCE_TO_ENTRANCE)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"- TakeEntrance entrancePoint {entrancePoint}, exit {entrance.GetExitPointFrom(ai.transform.position)}");
                ai.SyncTeleportIntern(entrance.GetExitPointFrom(ai.transform.position), !ai.isOutside, true);
                return true;
            }

            return false;
        }

        private bool CloseEnoughOfCurrentPoint(InternAI ai, Vector3 currentPoint)
        {
            float sqrHorizontalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(1, 0, 1)).sqrMagnitude;
            float sqrVerticalDistance = Vector3.Scale(currentPoint - ai.transform.position, new Vector3(0, 1, 0)).sqrMagnitude;
            if (sqrHorizontalDistance < MIN_DISTANCE_HOR * MIN_DISTANCE_HOR
                && sqrVerticalDistance < Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                return true;
            }

            return false;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Priority state
            // Stop talking and voice new state
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FollowingPlayer,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
