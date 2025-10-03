using LethalInternship.SharedAbstractions.Parameters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IDJKPoint : ICloneable
    {
        int Id { get; set; }
        List<(int idNeighbor, Vector3 neighborPos, float weight)> Neighbors { get; }


        Vector3 GetNeighborPos(int idNeighbor);
        void SetNeighborPos(int idNeighbor, Vector3 newPos);
        void SetNeighbors(List<(int idNeighbor, Vector3 neighborPos, float weight)> neighbors);

        bool IsNeighborExist(int idNeighbor);
        bool TryAddToNeighbors(int idNeighborToAdd, Vector3 neighborToAddPos, float weight);

        Vector3[] GetAllPoints();
        Vector3 GetClosestPointTo(Vector3 point);
        Vector3[] GetNearbyPoints(Vector3 point);

        IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess);
    }
}
