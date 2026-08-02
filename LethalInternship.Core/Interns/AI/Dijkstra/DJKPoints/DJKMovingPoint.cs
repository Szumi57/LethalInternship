using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.Pools;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKMovingPoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public string Name { get; set; }
        public Transform Transform { get; set; }

        private InstructionCalculatePathNoPartialsSamplePos instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();

        public DJKMovingPoint()
            : base()
        {
            this.Transform = null!;
            this.Name = string.Empty;
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            instruction = InternManager.Instance.Pools.Get<InstructionCalculatePathNoPartialsSamplePos>();
            instruction.Initialize(idBatch,
                                   instructionToProcess.groupId,
                                   start: instructionToProcess.start,
                                   target: instructionToProcess.target,
                                   startDJKPoint: instructionToProcess.startDJKPoint,
                                   targetDJKPoint: instructionToProcess.targetDJKPoint,
                                   0f,
                                   fromId: instructionToProcess.startDJKPoint.Id,
                                   toId: instructionToProcess.targetDJKPoint.Id,
                                   resultCallback: instructionToProcess.resultCallback);

            return instruction;
        }

        public override IEnumerable<Vector3> GetAllPoints()
        {
            pointsResults.Clear();
            pointsResults.Add(Transform.position);
            return pointsResults;
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            return Transform.position;
        }

        public override IEnumerable<Vector3> GetNearbyPoints(Vector3 point)
        {
            pointsResults.Clear();
            if (Mathf.Abs(Transform.position.y - point.y) <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                pointsResults.Add(Transform.position);

            return pointsResults;
        }

        public override string ToString()
        {
            _pathSb.Clear();

            _pathSb.Append("DJKMovingPoint \"");
            _pathSb.Append(Name);
            _pathSb.Append("\" id:");
            _pathSb.Append(Id);
            _pathSb.Append(", transform ");
            _pathSb.Append(Transform.name);
            _pathSb.Append(" pos: ");
            _pathSb.Append(Transform.position);

            return _pathSb.ToString();
        }

        public override void ReturnToPool(IPoolManager pool)
        {
            pool.Return(this);
        }

        public void CopyFrom(DJKMovingPoint other)
        {
            Id = other.Id;
            Transform = other.Transform;
            Name = other.Name;
        }

        public override IDJKPoint Clone(IPoolManager pool)
        {
            var clone = pool.Get<DJKMovingPoint>();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
