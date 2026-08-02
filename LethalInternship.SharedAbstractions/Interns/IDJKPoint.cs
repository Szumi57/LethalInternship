using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.Pools;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IDJKPoint
    {
        int Id { get; set; }

        IEnumerable<Vector3> GetAllPoints();
        Vector3 GetClosestPointTo(Vector3 point);
        IEnumerable<Vector3> GetNearbyPoints(Vector3 point);

        IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess);

        string ToString();

        IDJKPoint Clone(IPoolManager pool);
        void ReturnToPool(IPoolManager pool);
    }
}
