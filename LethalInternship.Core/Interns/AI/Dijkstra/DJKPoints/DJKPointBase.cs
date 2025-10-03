using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public abstract class DJKPointBase : IDJKPoint
    {
        public int Id { get; set; }
        public List<(int idNeighbor, Vector3 neighborPos, float weight)> Neighbors { get; protected set; }

        public abstract object Clone();
        public abstract IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess);
        public abstract Vector3[] GetAllPoints();
        public abstract Vector3 GetClosestPointTo(Vector3 point);
        public abstract Vector3[] GetNearbyPoints(Vector3 point);

        public Vector3 GetNeighborPos(int idNeighbor)
        {
            return Neighbors.First(x => x.idNeighbor == idNeighbor).neighborPos;
        }

        public bool IsNeighborExist(int idNeighbor)
        {
            return Neighbors.Any(x => x.idNeighbor == idNeighbor);
        }

        public void SetNeighbors(List<(int idNeighbor, Vector3 neighborPos, float weight)> neighbors)
        {
            Neighbors = neighbors;
        }

        public void SetNeighborPos(int idNeighbor, Vector3 newPos)
        {
            var neighbor = Neighbors.First(x => x.idNeighbor == idNeighbor);
            neighbor.neighborPos = newPos;
        }

        public bool TryAddToNeighbors(int idNeighborToAdd, Vector3 neighborToAddPos, float weight)
        {
            if (!Neighbors.Any(x => x.idNeighbor == idNeighborToAdd))
            {
                Neighbors.Add((idNeighborToAdd, neighborToAddPos, weight));
                return true;
            }

            return false;
        }
    }
}
