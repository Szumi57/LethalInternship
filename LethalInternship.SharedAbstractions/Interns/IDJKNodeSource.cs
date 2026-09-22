using System.Collections.Generic;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IDJKNodeSource
    {
        int NodeCount { get; }
        IEnumerable<DJKNeighbor> GetNeighbors(int nodeId);
    }
}
