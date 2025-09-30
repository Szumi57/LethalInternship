using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CalculateNextPathPoint : IBTAction
    {
        private BTContext currentContext = null!;
        private GraphController graph = null!;

        private TimedCalculatePath calculateDestinationPathTimed = new TimedCalculatePath();
        private TimedCalculatePath calculateNextPointPathTimed = new TimedCalculatePath();

        private DJKPositionPoint source = null!;
        private DJKPositionPoint dest = null!;

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
                // Go directly to destination
                //PluginLoggerHook.LogDebug?.Invoke($"- Destination reachable");
                context.PathController.SetNextPointToDestination();
                return BehaviourTreeStatus.Success;
            }

            // Check if current PathPoint reachable
            path = calculateNextPointPathTimed.GetPath(ai, context.PathController.GetCurrentPoint(ai.transform.position));
            if (!path.IsDirectlyReachable)
            {
                // Need to calculate further
                CalculateGraphPath(context);
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathComplete
                || (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete))
            {
                if (path.PathStatus == NavMeshPathStatus.PathInvalid && ai.agent.path.status == NavMeshPathStatus.PathComplete)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"** current PathPoint reachable path.status {path.PathStatus} | agent {ai.agent.path.status} isPathStale {ai.agent.isPathStale}");
                }

                // Go
                return BehaviourTreeStatus.Success;
            }
            else if (path.PathStatus == NavMeshPathStatus.PathPartial)
            {
                context.PathController.SetCurrentPoint(new DJKPositionPoint(path.Path.corners[^1], "PartialPoint"));

                // Try to still calculate
                if (context.PathController.IsPathNotValid())
                {
                    CalculateGraphPath(context);
                }
                return BehaviourTreeStatus.Success;
            }

            // Need to calculate further
            CalculateGraphPath(context);
            return BehaviourTreeStatus.Success;
        }

        private void CalculateGraphPath(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Get entrances graph
            GraphController? GraphEntrances = InternManager.Instance.GetGraphEntrances();
            if (GraphEntrances == null || GraphEntrances.DJKPoints.Count == 0)
            {
                PluginLoggerHook.LogDebug?.Invoke($"- GetGraphEntrances not available yet/empty");
                return;
            }

            graph = new GraphController(GraphEntrances);

            // Add source and dest
            Vector3 internPos = ai.transform.position;
            if (NavMesh.SamplePosition(internPos, out NavMeshHit hitEnd, 2f, NavMesh.AllAreas))
            {
                PluginLoggerHook.LogDebug?.Invoke($"Using internpos sampled position, diff dist {(internPos - hitEnd.position).magnitude}");
                internPos = hitEnd.position;
            }

            source = new DJKPositionPoint(internPos, "Intern pos");
            dest = new DJKPositionPoint(context.PathController.GetDestination().GetClosestPointTo(internPos), "Destination");
            graph.AddPoint(source);
            graph.AddPoint(dest);

            // Calculate Neighbors
            CalculateNeighbors((int)ai.Npc.playerClientId);
        }

        private void CalculateNeighbors(int idBatch)
        {
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
            PluginLoggerHook.LogDebug?.Invoke($"------- {graph}");

            // Get full path
            currentContext.PathController.SetNewPath(Dijkstra.Dijkstra.CalculatePath(graph.DJKPoints, source, dest));

            // log
            PluginLoggerHook.LogDebug?.Invoke($"======= {currentContext.PathController}");
        }
    }
}
