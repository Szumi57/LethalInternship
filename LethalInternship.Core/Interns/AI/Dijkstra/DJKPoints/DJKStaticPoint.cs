using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKStaticPoint : IDJKPoint
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public List<(int idNeighbor, Vector3 neighborPos, float weight)> Neighbors { get; private set; }

        public DJKStaticPoint(Vector3 position)
        {
            Id = 0;
            Position = position;
            Neighbors = new List<(int idNeighbor, Vector3 neighborPos, float weight)>();
            Name = string.Empty;
        }

        public DJKStaticPoint(Vector3 position, string name)
        {
            Name = name;
            Id = 0;
            Position = position;
            Neighbors = new List<(int idNeighbor, Vector3 neighborPos, float weight)>();
        }

        public object Clone()
        {
            var copy = new DJKStaticPoint(Position, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public bool IsNeighborExist(int idNeighbor)
        {
            return Neighbors.Any(x => x.idNeighbor == idNeighbor);
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

        public Vector3[] GetAllPoints()
        {
            return new Vector3[] { Position };
        }

        public Vector3 GetClosestPointTo(Vector3 point)
        {
            return Position;
        }

        public Vector3 GetNeighborPos(int idNeighbor)
        {
            return Neighbors.First(x => x.idNeighbor == idNeighbor).neighborPos;
        }

        public void SetNeighborPos(int idNeighbor, Vector3 newPos)
        {
            var neighbor = Neighbors.First(x => x.idNeighbor == idNeighbor);
            neighbor.neighborPos = newPos;
        }

        public Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3> { Position };
            return points
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .ToArray();
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

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.idNeighbor}({(int)Mathf.Sqrt(x.weight)})"));

            return $"DJKStaticPoint \"{Name}\" id:{Id}, Position: {Position}, Neighbors {{{neighborsString}}}";
        }
    }
}
