using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.Pools;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public abstract class DJKPointBase : IDJKPoint
    {
        public int Id { get; set; }

        public abstract IDJKPoint Clone(IPoolManager pool);
        public abstract IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess);
        public abstract IEnumerable<Vector3> GetAllPoints();
        public abstract Vector3 GetClosestPointTo(Vector3 point);
        public abstract IEnumerable<Vector3> GetNearbyPoints(Vector3 point);
        public abstract void ReturnToPool(IPoolManager pool);
    }
}
