using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKStaticPoint : DJKPointBase
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }

        public DJKStaticPoint(Vector3 position)
            : base()
        {
            Position = position;
            Name = string.Empty;
        }

        public DJKStaticPoint(Vector3 position, string name)
            : base()
        {
            Name = name;
            Position = position;
        }

        public override object Clone()
        {
            var copy = new DJKStaticPoint(Position, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public override Vector3[] GetAllPoints()
        {
            return new Vector3[] { Position };
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            return Position;
        }

        public override Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3> { Position };
            return points
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .ToArray();
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
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
