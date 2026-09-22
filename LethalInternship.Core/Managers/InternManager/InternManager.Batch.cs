using LethalInternship.Core.Interns.AI.Batches;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region GraphEntrances

        private TimedGetGraphEntrances getGraphEntrancesTimed = new TimedGetGraphEntrances();

        public GraphController GetGraphEntrances()
        {
            return getGraphEntrancesTimed.GetGraphEntrances();
        }

        #endregion

        #region Graph and path calculation

        private int nextInstructionGroupId = 1;
        public int GetNewInstructionGroupId() => nextInstructionGroupId++;

        private int maxBatchesPerFrame = 1;
        private int maxInstructionsPerFrame = 1;
        private int currentBatch = -2;

        private Dictionary<int, BatchRequest> activeBatches = new Dictionary<int, BatchRequest>();
        private readonly List<(BatchRequest batch, float dist)> sortedBatches = new List<(BatchRequest batch, float dist)>();

        public void RequestBatch(int idBatch, List<IInstruction> instructions, Action? onBatchComplete = null)
        {
            CancelBatch(idBatch);
            BatchRequest batch = Pools.Get<BatchRequest>();
            batch.Initialize(idBatch, instructions, onBatchComplete);
            activeBatches[idBatch] = batch;
        }

        private void ProcessCalculatePathQueue()
        {
            currentBatch = -2;
            if (activeBatches.Count == 0) return;

            int processedBatches = 0;
            int processedInstructions = 0;

            sortedBatches.Clear();
            foreach (var batch in activeBatches.Values)
                sortedBatches.Add((batch, GetDistanceFromClosestPlayer(batch)));

            sortedBatches.Sort((a, b) => a.dist.CompareTo(b.dist));
            foreach (var item in sortedBatches)
            {
                var batch = item.batch;
                if (processedBatches >= maxBatchesPerFrame) break;
                if (processedInstructions >= maxInstructionsPerFrame) break;

                // Has remaining instructions ?
                if (!batch.HasRemaining)
                {
                    batch.onBatchComplete?.Invoke();
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
                    CancelBatch(idBatch);
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
                CancelBatch(idBatch);
        }

        public void CancelBatch(int idBatch)
        {
            if (activeBatches.Remove(idBatch, out BatchRequest batch))
            {
                batch.Reset();
                Pools.Return(batch);
            }
        }

        public int GetCurrentBatch()
        {
            return currentBatch;
        }

        private void ExecuteInstruction(IInstruction instr)
        {
            instr.Execute();
            //Debug.Log($"Instruction ReleaseInPool idBatch={instr.IdBatch}");
            instr.ReleaseInPool();
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
