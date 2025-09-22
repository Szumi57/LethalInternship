using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.Dijkstra.PathRequests
{
    public class PathBatchRequest
    {
        public int id;
        public List<PathInstruction> instructions;
        public int currentIndex;

        public PathBatchRequest(int id, List<PathInstruction> instructions)
        {
            this.id = id;
            this.instructions = instructions ?? new List<PathInstruction>();
            currentIndex = 0;
        }

        public bool HasRemaining => currentIndex < instructions.Count;
        public PathInstruction CurrentInstruction => instructions[currentIndex];
        public void Advance() => currentIndex++;
    }
}
