using LethalInternship.Core.Interns.AI.Batches;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region Graph and path calculation

        private TimedGetGraphEntrances getGraphEntrancesTimed = null!;

        private int nextInstructionGroupId = 1;
        public int GetNewInstructionGroupId() => nextInstructionGroupId++;

        public GraphController? GetGraphEntrances()
        {
            if (getGraphEntrancesTimed == null)
            {
                getGraphEntrancesTimed = new TimedGetGraphEntrances();
            }

            return getGraphEntrancesTimed.GetGraphEntrances();
        }

        private int maxBatchesPerFrame = 1;
        private int maxInstructionsPerFrame = 1;
        private int currentBatch = -2;

        private Dictionary<int, BatchRequest> activeBatches = new Dictionary<int, BatchRequest>();

        public void RequestBatch(int idBatch, List<IInstruction> instructions, Action? onBatchComplete = null)
        {
            var newBatch = new BatchRequest(idBatch, instructions, onBatchComplete);
            activeBatches[idBatch] = newBatch;
        }

        private void ProcessCalculatePathQueue()
        {
            currentBatch = -2;
            if (activeBatches.Count == 0) return;

            int processedBatches = 0;
            int processedInstructions = 0;

            var sorted = activeBatches.Values
                        .OrderBy(b => GetDistanceFromClosestPlayer(b))
                        .ToList();

            foreach (var batch in sorted)
            {
                if (processedBatches >= maxBatchesPerFrame) break;
                if (processedInstructions >= maxInstructionsPerFrame) break;

                // Has remaining instructions ?
                if (!batch.HasRemaining)
                {
                    batch.onBatchComplete?.Invoke();
                    activeBatches.Remove(batch.id);
                    CancelBatch(batch.id);
                    continue;
                }

                // Execute one instruction only
                var instr = batch.CurrentInstruction;
                ExecuteInstruction(instr);
                batch.Advance();

                processedInstructions++;
                processedBatches++;
                currentBatch = batch.id;

                if (!batch.HasRemaining)
                {
                    batch.onBatchComplete?.Invoke();
                    CancelBatch(batch.id);
                }
            }
        }

        public void CancelGroup(int idBatch, int groupId)
        {
            if (activeBatches.TryGetValue(idBatch, out var batch))
            {
                batch.CancelInstructionsInGroup(groupId);

                if (!batch.HasRemaining)
                    activeBatches.Remove(idBatch);
            }
        }

        public void CancelGroupGlobal(int groupId)
        {
            var toRemove = new List<int>();
            foreach (var kvp in activeBatches)
            {
                kvp.Value.CancelInstructionsInGroup(groupId);
                if (!kvp.Value.HasRemaining)
                    toRemove.Add(kvp.Key);
            }
            foreach (var idBatch in toRemove)
                activeBatches.Remove(idBatch);
        }

        public void CancelBatch(int idBatch)
        {
            activeBatches.Remove(idBatch);
        }

        public int GetCurrentBatch()
        {
            return currentBatch;
        }

        private void ExecuteInstruction(IInstruction instr)
        {
            instr.Execute();
        }

        private float GetDistanceFromClosestPlayer(BatchRequest batch)
        {
            if (!batch.HasRemaining) return float.MaxValue;
            if (batch.id < 0) return float.MinValue;

            IInternAI? internAI = GetInternAI(batch.id);
            if (internAI == null)
            {
                return float.MaxValue;
            }

            return internAI.GetClosestPlayerDistance();
        }

        #endregion
    }
}
