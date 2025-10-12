using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    internal class DJKItemPoint : DJKPointBase
    {
        public string Name { get; set; }
        public Transform Transform { get; set; }

        public float grabDistance;
        public int checkPoints = 8;

        public DJKItemPoint(Transform transform, float grabDistance)
            : base()
        {
            this.Transform = transform;
            this.Name = string.Empty;
            this.grabDistance = grabDistance;
        }

        public DJKItemPoint(Transform transform, float grabDistance, string name)
            : base()
        {
            this.Transform = transform;
            this.Name = name;
            this.grabDistance = grabDistance;
        }

        public override object Clone()
        {
            var copy = new DJKItemPoint(Transform, grabDistance, Name);
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            return new InstructionCalculatePathItems(
                                idBatch,
                                instructionToProcess.groupId,
                                start: instructionToProcess.start,
                                target: instructionToProcess.target,
                                startDJKPoint: instructionToProcess.startDJKPoint,
                                targetDJKPoint: instructionToProcess.targetDJKPoint,
                                samplePosDist: 4f);
        }

        private Vector3[] GetWorldPoints()
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < checkPoints; i++)
            {
                float angle = (360f / checkPoints) * i;
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                points.Add(Transform.position + dir * grabDistance);
            }

            return points.ToArray();
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

            return $"DJKItemPoint \"{Name}\" id:{Id}, transform {Transform.name} pos: {Transform.position}, Neighbors {{{neighborsString}}}";
        }
    }
}
