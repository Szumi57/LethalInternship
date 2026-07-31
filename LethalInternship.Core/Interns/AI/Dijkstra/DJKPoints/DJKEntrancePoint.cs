using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKEntrancePoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public EntranceTeleport Entrance1 { get; set; }
        public EntranceTeleport? Entrance2 { get; set; }

        private InstructionCalculatePathNoPartials instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();

        public DJKEntrancePoint()
        {
            Entrance1 = null!;
        }

        public DJKEntrancePoint(EntranceTeleport entrance)
            : base()
        {
            Entrance1 = entrance;
        }

        public bool TryAddOtherEntrance(EntranceTeleport entrance2)
        {
            //PluginLoggerHook.LogDebug?.Invoke($"id: {Id}, {entrance2.entrancePoint} =? {Entrance1.exitPoint}");
            if (entrance2 != Entrance1
                && entrance2.entranceId == Entrance1.entranceId)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"new entrance2 !!!");
                Entrance2 = entrance2;
                return true;
            }

            return false;
        }

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
            if (Entrance2 == null)
            {
                return Entrance1.entrancePoint.position;
            }

            if ((point - Entrance1.entrancePoint.position).sqrMagnitude < (point - Entrance2.entrancePoint.position).sqrMagnitude)
            {
                return Entrance1.entrancePoint.position;
            }
            else
            {
                return Entrance2.entrancePoint.position;
            }
        }

        public override IEnumerable<Vector3> GetAllPoints()
        {
            pointsResults.Clear();
            pointsResults.Add(Entrance1.entrancePoint.position);
            if (Entrance2 != null)
            {
                pointsResults.Add(Entrance2.entrancePoint.position);
            }

            return pointsResults;
        }

        public override IEnumerable<Vector3> GetNearbyPoints(Vector3 point)
        {
            pointsResults.Clear();
            pointsResults.Add(Entrance1.entrancePoint.position);
            if (Entrance2 != null)
            {
                pointsResults.Add(Entrance2.entrancePoint.position);
            }

            // Filter
            for (int i = pointsResults.Count - 1; i >= 0; i--)
            {
                if (Mathf.Abs(pointsResults[i].y - point.y) > Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                    pointsResults.RemoveAt(i);
            }

            // Sort
            pointsResults.Sort((a, b) =>
            {
                float da = (a - point).sqrMagnitude;
                float db = (b - point).sqrMagnitude;
                return da.CompareTo(db);
            });

            return pointsResults;
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
