using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.Pools;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKEntrancePoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public EntranceTeleport Entrance1 { get; set; } = null!;
        public EntranceTeleport? Entrance2 { get; set; }

        private InstructionCalculatePathNoPartials instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();

        public DJKEntrancePoint() { }

        public Vector3 GetExitPointFrom(Vector3 point)
        {
            if (Entrance2 == null)
            {
                return Entrance1.entrancePoint.position;
            }

            if ((point - Entrance1.entrancePoint.position).sqrMagnitude < (point - Entrance2.entrancePoint.position).sqrMagnitude)
            {
                return Entrance2.entrancePoint.position;
            }
            else
            {
                return Entrance1.entrancePoint.position;
            }
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            return Entrance1.entrancePoint.position;
        }

        public override IEnumerable<Vector3> GetAllPoints()
        {
            pointsResults.Clear();
            if (Entrance1.entrancePoint != null)
            {
                // Can be null after first moon
                pointsResults.Add(Entrance1.entrancePoint.position);
            }
            return pointsResults;
        }

        public override IEnumerable<Vector3> GetNearbyPoints(Vector3 point)
        {
            // only entrance1
            return GetAllPoints();
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            instruction = InternManager.Instance.Pools.Get<InstructionCalculatePathNoPartials>();
            instruction.Initialize(idBatch,
                                   instructionToProcess.groupId,
                                   start: instructionToProcess.start,
                                   target: instructionToProcess.target,
                                   startDJKPoint: instructionToProcess.startDJKPoint,
                                   targetDJKPoint: instructionToProcess.targetDJKPoint,
                                   samplePosDist: 0f,
                                   fromId: instructionToProcess.startDJKPoint.Id,
                                   toId: instructionToProcess.targetDJKPoint.Id,
                                   resultCallback: instructionToProcess.resultCallback);

            return instruction;
        }

        public override string ToString()
        {
            _pathSb.Clear();

            _pathSb.Append("DJKEntrancePoint id:");
            _pathSb.Append(Id);
            _pathSb.Append(", Entrance1:{");
            _pathSb.Append(Entrance1.entrancePoint.position);
            _pathSb.Append("}, Entrance2: ");
            if (Entrance2 == null)
                _pathSb.Append("null");
            else
            {
                _pathSb.Append("{");
                _pathSb.Append(Entrance2.entrancePoint.position);
                _pathSb.Append("}");
            }

            return _pathSb.ToString();
        }

        public override void ReturnToPool(IPoolManager pool)
        {
            pool.Return(this);
        }

        public void CopyFrom(DJKEntrancePoint other)
        {
            Id = other.Id;
            Entrance1 = other.Entrance1;
            Entrance2 = other.Entrance2;
        }

        public override IDJKPoint Clone(IPoolManager pool)
        {
            var clone = pool.Get<DJKEntrancePoint>();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
