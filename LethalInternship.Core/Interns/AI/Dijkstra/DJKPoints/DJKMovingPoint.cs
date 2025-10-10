using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKMovingPoint : DJKPointBase
    {
        public string Name { get; set; }
        public Transform Transform { get; set; }

        public DJKMovingPoint(Transform transform)
            : base()
        {
            this.Transform = transform;
            Name = string.Empty;
        }

        public DJKMovingPoint(Transform transform, string name)
            : base()
        {
            this.Transform = transform;
            this.Name = name;
        }

        public override object Clone()
        {
            var copy = new DJKMovingPoint(Transform, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
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

        public override Vector3[] GetAllPoints()
        {
            return new Vector3[] { Transform.position };
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            return Transform.position;
        }

        public override Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3> { Transform.position };
            return points
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .ToArray();
        }

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.idNeighbor}({(int)Mathf.Sqrt(x.weight)})"));

            return $"DJKMovingPoint \"{Name}\" id:{Id}, transform {Transform.name} pos: {Transform.position}, Neighbors {{{neighborsString}}}";
        }
    }
}
