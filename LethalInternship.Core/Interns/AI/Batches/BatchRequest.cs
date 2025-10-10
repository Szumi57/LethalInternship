using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.Batches
{
    public class BatchRequest
    {
        public int id;
        public List<IInstruction> instructions;
        public int currentIndex;
        public Action? onBatchComplete;

        public BatchRequest(int id, List<IInstruction> instructions, Action? onBatchComplete = null)
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
            var newList = new List<IInstruction>();
            for (int i = 0; i <= currentIndex; i++)
            {
                newList.Add(instructions[i]);
            }

            for (int i = currentIndex + 1; i < instructions.Count; i++)
            {
                if (instructions[i].GroupId != groupId)
                {
                    newList.Add(instructions[i]);
                }
            }

            instructions = newList;
        }
    }
}
