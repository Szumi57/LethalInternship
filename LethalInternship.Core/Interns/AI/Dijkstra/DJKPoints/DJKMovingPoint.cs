using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKMovingPoint : IDJKPoint
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Transform Transform { get; set; }

        public List<(int idNeighbor, Vector3 neighborPos, float weight)> Neighbors { get; private set; }

        public DJKMovingPoint(Transform transform)
        {
            Id = 0;
            this.Transform = transform;
            Neighbors = new List<(int idNeighbor, Vector3 neighborPos, float weight)>();
            Name = string.Empty;
        }

        public DJKMovingPoint(Transform transform, string name)
        {
            Id = 0;
            this.Transform = transform;
            this.Name = name;
            Neighbors = new List<(int idNeighbor, Vector3 neighborPos, float weight)>();
        }

        public object Clone()
        {
            var copy = new DJKMovingPoint(Transform, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            return new InstructionCalculatePathWithSamplePos(
                                idBatch,
                                instructionToProcess.groupId,
                                start: instructionToProcess.start,
                                target: instructionToProcess.target,
                                startDJKPoint: instructionToProcess.startDJKPoint,
                                targetDJKPoint: instructionToProcess.targetDJKPoint,
                                samplePosDist: 2f);
        }

        public Vector3[] GetAllPoints()
        {
            return new Vector3[] { Transform.position };
        }

        public Vector3 GetClosestPointTo(Vector3 point)
        {
            return Transform.position;
        }

        public Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3> { Transform.position };
            return points
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .ToArray();
        }

        public Vector3 GetNeighborPos(int idNeighbor)
        {
            return Neighbors.First(x => x.idNeighbor == idNeighbor).neighborPos;
        }

        public bool IsNeighborExist(int idNeighbor)
        {
            return Neighbors.Any(x => x.idNeighbor == idNeighbor);
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

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.idNeighbor}({(int)Mathf.Sqrt(x.weight)})"));

            return $"DJKMovingPoint \"{Name}\" id:{Id}, transform {Transform.name} pos: {Transform.position}, Neighbors {{{neighborsString}}}";
        }
    }
}
