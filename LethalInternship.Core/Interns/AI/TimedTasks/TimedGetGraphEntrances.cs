using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGraphEntrances
    {
        private GraphController? graph = null!;
        private GraphController tempGraph = null!;

        private long timer = 10000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        private bool IsCalculating = false;

        public GraphController? GetGraphEntrances()
        {
            if (!NeedToRecalculate()
                || IsCalculating)
            {
                if (IsCalculating)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"CalculateGraphEntrances Calculating");
                }

                if (graph != null)
                {
                    graph.CleanNeighbors();
                }
                return graph;
            }

            // Construct graph entrances
            EntranceTeleport[] entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            tempGraph = CalculateGraphEntrances(entrancesTeleportArray);

            // Calculate Neighbors
            CalculateNeighbors();

            return graph;
        }

        private bool NeedToRecalculate()
        {
            long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
            if (elapsedTime > timer)
            {
                lastTimeCalculate = DateTime.Now.Ticks;
                return true;
            }
            else
            {
                return false;
            }
        }

        private GraphController CalculateGraphEntrances(EntranceTeleport[] entrancesTeleportArray)
        {
            GraphController graphEntrancesController = new GraphController();

            // List<DJKPoint> init with entrances
            foreach (var entrance in entrancesTeleportArray)
            {
                bool newDJKPoint = true;
                foreach (var DJKP in graphEntrancesController.DJKPoints)
                {
                    if (((DJKEntrancePoint)DJKP).TryAddOtherEntrance(entrance))
                    {
                        newDJKPoint = false;
                        break;
                    }
                }

                if (newDJKPoint)
                {
                    graphEntrancesController.AddPoint(new DJKEntrancePoint(entrance));
                }
            }

            return graphEntrancesController;
        }

        private void CalculateNeighbors()
        {
            int idBatch = -1;
            List<InstructionParameters> instructions = Dijkstra.Dijkstra.GenerateWorkCalculateNeighbors(tempGraph.DJKPoints);
            List<IInstruction> instructionsToProcess = new List<IInstruction>();
            foreach (var instrParams in instructions)
            {
                instructionsToProcess.Add(instrParams.targetDJKPoint.GenerateInstruction(idBatch, instrParams));
            }

            InternManager.Instance.RequestBatch(idBatch, instructionsToProcess, OnBatchComplete);
        }

        private void OnBatchComplete()
        {
            graph = new GraphController(tempGraph);
            IsCalculating = false;
        }
    }
}
