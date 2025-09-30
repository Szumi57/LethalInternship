using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKPositionPoint : IDJKPoint
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public List<(IDJKPoint neighbor, float weight)> Neighbors { get; }

        public DJKPositionPoint(Vector3 position)
        {
            Id = 0;
            Position = position;
            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
            Name = string.Empty;
        }

        public DJKPositionPoint(Vector3 position, string name)
        {
            Name = name;
            Id = 0;
            Position = position;
            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
        }

        public bool IsNeighborExist(IDJKPoint neighbor)
        {
            return Neighbors.Any(x => x.neighbor.Id == neighbor.Id);
        }

        public float? GetNeighborDistanceIfExist(IDJKPoint neighbor)
        {
            if (IsNeighborExist(neighbor))
            {
                return Neighbors.FirstOrDefault(x => x.neighbor.Id == neighbor.Id).weight;
            }
            return null;
        }

        public bool TryAddToNeighbors(IDJKPoint neighborToAdd, float weight)
        {
            if (!Neighbors.Any(x => x.neighbor.Id == neighborToAdd.Id))
            {
                Neighbors.Add((neighborToAdd, weight));
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

        public Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3> { Position };
            return points
                        .Where(p => (p - point).sqrMagnitude <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
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
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.neighbor.Id}({(int)Mathf.Sqrt(x.weight)})"));

            return $"DJKSimplePoint \"{Name}\" id:{Id}, Position: {Position}, Neighbors {{{neighborsString}}}";
        }
    }
}
