using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKVehiclePoint : DJKPointBase
    {
        public string Name { get; set; }
        public Transform Transform { get; set; }

        private Vector3[] points = new Vector3[]{ Const.LEFT_FRONT_POS_CRUISER,
                                                  Const.RIGHT_FRONT_POS_CRUISER,
                                                  Const.LEFT_CENTER_POS_CRUISER,
                                                  Const.RIGHT_CENTER_POS_CRUISER,
                                                  Const.LEFT_BACK_POS_CRUISER,
                                                  Const.RIGHT_BACK_POS_CRUISER };

        public DJKVehiclePoint(Transform transform)
            : base()
        {
            this.Transform = transform;
            this.Name = string.Empty;
        }

        public DJKVehiclePoint(Transform transform, string name)
            : base()
        {
            this.Transform = transform;
            this.Name = name;
        }

        public override object Clone()
        {
            var copy = new DJKVehiclePoint(Transform, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            return new InstructionCalculatePathMultPointsSamplePos(
                                idBatch,
                                instructionToProcess.groupId,
                                start: instructionToProcess.start,
                                target: instructionToProcess.target,
                                startDJKPoint: instructionToProcess.startDJKPoint,
                                targetDJKPoint: instructionToProcess.targetDJKPoint,
                                samplePosDist: 1f);
        }

        private Vector3[] GetWorldPoints()
        {
            return points.Select(x => Transform.position + Transform.rotation * x).ToArray();
        }

        public override Vector3[] GetAllPoints()
        {
            return GetWorldPoints();
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            var worldPoints = GetWorldPoints()
                                .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                                .OrderBy(p => (p - point).sqrMagnitude);
            if (!worldPoints.Any())
            {
                return Transform.position;
            }

            return worldPoints.First();
        }

        public override Vector3[] GetNearbyPoints(Vector3 point)
        {
            return GetWorldPoints()
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .OrderBy(p => (p - point).sqrMagnitude)
                        .ToArray();
        }

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.idNeighbor}({(int)Mathf.Sqrt(x.weight)})"));

            return $"DJKVehiclePoint \"{Name}\" id:{Id}, transform {Transform.name} pos: {Transform.position}, Neighbors {{{neighborsString}}}";
        }
    }
}
