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
    public class DJKStaticPoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public string Name { get; set; }
        public Vector3 Position { get; set; }

        private InstructionCalculatePathNoPartialsSamplePos instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();

        public DJKStaticPoint()
            : base()
        {
            Position = Vector3.zero;
            Name = string.Empty;
        }

        public DJKStaticPoint(string name)
            : base()
        {
            Name = name;
            Position = Vector3.zero;
        }

        public override IEnumerable<Vector3> GetAllPoints()
        {
            pointsResults.Clear();
            pointsResults.Add(Position);
            return pointsResults;
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            return Position;
        }

        public override IEnumerable<Vector3> GetNearbyPoints(Vector3 point)
        {
            pointsResults.Clear();
            if (Mathf.Abs(Position.y - point.y) <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                pointsResults.Add(Position);

            return pointsResults;
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

        public override string ToString()
        {
            _pathSb.Clear();

            _pathSb.Append("DJKStaticPoint \"");
            _pathSb.Append(Name);
            _pathSb.Append("\" id:");
            _pathSb.Append(Id);
            _pathSb.Append(" pos: ");
            _pathSb.Append(Position);

            return _pathSb.ToString();
        }

        public override void ReturnToPool(IPoolManager pool)
        {
            pool.Return(this);
        }

        public void CopyFrom(DJKStaticPoint other)
        {
            Id = other.Id;
            Name = other.Name;
            Position = other.Position;
        }

        public override IDJKPoint Clone(IPoolManager pool)
        {
            var clone = pool.Get<DJKStaticPoint>();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
