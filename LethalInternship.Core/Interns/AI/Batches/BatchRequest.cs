using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Batches
{
    public class BatchRequest
    {
        public int id;
        public List<IInstruction> instructions = null!;
        public int currentIndex;
        public Action? onBatchComplete;

        public void Initialize(int id, List<IInstruction> instructions, Action? onBatchComplete = null)
        {
            this.id = id;
            this.instructions = instructions ?? new List<IInstruction>();
            currentIndex = 0;
            this.onBatchComplete = onBatchComplete;
        }

        public bool HasRemaining => currentIndex < instructions.Count;
        public IInstruction CurrentInstruction => instructions[currentIndex];
        public void Advance() => currentIndex++;

        public void CancelInstructionsInGroup(int groupId)
        {
            int write = currentIndex + 1;

            for (int read = currentIndex + 1; read < instructions.Count; read++)
            {
                IInstruction instruction = instructions[read];

                if (instruction.GroupId == groupId)
                {
                    instruction.ReleaseInPool();
                }
                else
                {
                    instructions[write++] = instruction;
                }
            }

            instructions.RemoveRange(write, instructions.Count - write);
        }

        public void Reset()
        {
            Debug.Log("Pools.LogStats CancelBatch -------------------");
            InternManager.Instance.Pools.LogStats();
            Debug.Log("----------------------------------");

            while (this.HasRemaining)
            {
                Debug.Log($"CancelBatch idBatch={this.id} currentIndex={this.currentIndex} Count={this.instructions.Count}");
                this.CurrentInstruction.ReleaseInPool();
                this.Advance();
            }
            Debug.Log("Pools.LogStats CancelBatch -------------------");
            InternManager.Instance.Pools.LogStats();
            Debug.Log("----------------------------------");
            this.instructions.Clear();

            this.id = -2;
            currentIndex = 0;
            this.onBatchComplete = null;
        }
    }
}
