using LethalInternship.Core.Interns.AI.Batches.Instructions;
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

        public DJKPositionPoint(int id, Vector3 position)
        {
            Id = id;
            Position = position;
            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
            Name = string.Empty;
        }

        public DJKPositionPoint(int id, Vector3 position, string name)
        {
            Name = name;
            Id = id;
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

        public Vector3 GetClosestPointFrom(Vector3 point)
        {
            return Position;
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
