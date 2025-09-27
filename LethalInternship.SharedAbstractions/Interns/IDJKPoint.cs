using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IDJKPoint
    {
        int Id { get; set; }

        List<(IDJKPoint neighbor, float weight)> Neighbors { get; }

        bool IsNeighborExist(IDJKPoint neighbor);
        float? GetNeighborDistanceIfExist(IDJKPoint neighbor);
        bool TryAddToNeighbors(IDJKPoint neighborToAdd, float weight);

        Vector3[] GetAllPoints();
        Vector3 GetClosestPointFrom(Vector3 point);

        IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess);
    }
}
