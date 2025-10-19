using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CalculateNextPathPoint : IBTAction
    {
        private BTContext currentContext = null!;
        private GraphController graph = null!;

        private TimedCalculatePath calculateDestinationPathTimed = new TimedCalculatePath();
        private TimedCalculatePath calculateNextPointPathTimed = new TimedCalculatePath();

        public BehaviourTreeStatus Action(BTContext context)
        {
            currentContext = context;
            InternAI ai = context.InternAI;
            if (!ai.IsAgentInValidState())
            {
                return BehaviourTreeStatus.Success;
            }

            // Check if destination reachable
            TimedCalculatePathResponse path = calculateDestinationPathTimed.GetPath(ai, context.PathController.GetDestination().GetClosestPointTo(ai.transform.position));
            if (path.PathStatus == NavMeshPathStatus.PathComplete)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"- Destination reachable");
                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);
                // Go directly to destination
                context.PathController.SetNextPointToDestination();
                return BehaviourTreeStatus.Success;
            }

            // Check if current PathPoint reachable
            path = calculateNextPointPathTimed.GetPath(ai, context.PathController.GetCurrentPointPos(ai.transform.position));
            if (!path.IsDirectlyReachable)
            {
                // Need to calculate further
                CalculatePath(context);
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathComplete
                 || (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete))
            {
                // Path calculated invalid but agent path still valid

                //if (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete)
                //{
                //    PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status {path.PathStatus} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                //}

                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);

                // Go
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathPartial)
            {
                // Path calculated partial
                context.PathController.SetCurrentPoint(new DJKStaticPoint(path.Path.corners[^1], "PartialPoint"));

                // Try to still calculate
                if (!context.PathController.IsPathValid())
                {
                    CalculatePath(context);
                }

                DrawUtil.DrawPath(ai.LineRendererUtil, path.Path);

                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathPartial)
            {
                // Path calculated invalid but agent path partial
                context.PathController.SetCurrentPoint(new DJKStaticPoint(ai.agent.path.corners[^1], "PartialPoint"));

                // Try to still calculate
                if (!context.PathController.IsPathValid())
                {
                    CalculatePath(context);
                }

                DrawUtil.DrawPath(ai.LineRendererUtil, ai.agent.path);

                return BehaviourTreeStatus.Success;
            }

            // Need to calculate further
            CalculatePath(context);
            return BehaviourTreeStatus.Success;
        }

        private void CalculatePath(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Get entrances graph
            GraphController? GraphEntrances = InternManager.Instance.GetGraphEntrances();
            if (GraphEntrances == null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- CalculateNextPathPoint GetGraphEntrances not available yet");
                return;
            }

            graph = new GraphController(GraphEntrances);

            // Add source and dest
            graph.AddPoint(new DJKStaticPoint(Dijkstra.Dijkstra.GetSampledPos(ai.transform.position), $"{ai.Npc.playerUsername} pos"));
            graph.AddPoint(context.PathController.GetDestination());

            // Calculate Neighbors
            int idBatch = (int)ai.Npc.playerClientId;
            List<InstructionParameters> instructions = Dijkstra.Dijkstra.GenerateWorkCalculateNeighbors(graph.DJKPoints);
            List<IInstruction> instructionsToProcess = new List<IInstruction>();
            foreach (var instrParams in instructions)
            {
                instructionsToProcess.Add(instrParams.targetDJKPoint.GenerateInstruction(idBatch, instrParams));
            }

            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchCompleted);
        }

        private void OnBatchCompleted()
        {
            // log
            //PluginLoggerHook.LogDebug?.Invoke($"CalculateNextPathPoint ------- {graph}");

            // Get full path
            currentContext.PathController.SetNewPath(Dijkstra.Dijkstra.CalculatePath(graph.DJKPoints));

            // log
            //PluginLoggerHook.LogDebug?.Invoke($"CalculateNextPathPoint ======= {currentContext.PathController.GetFullPathString()}");
        }
    }
}
