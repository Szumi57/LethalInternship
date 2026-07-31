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
    public class DJKVehiclePoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public string Name { get; set; }
        public Transform Transform { get; set; }

        private Vector3[] points = new Vector3[]{ Const.LEFT_FRONT_POS_CRUISER,
                                                  Const.RIGHT_FRONT_POS_CRUISER,
                                                  Const.LEFT_CENTER_POS_CRUISER,
                                                  Const.RIGHT_CENTER_POS_CRUISER,
                                                  Const.LEFT_BACK_POS_CRUISER,
                                                  Const.RIGHT_BACK_POS_CRUISER };

        private InstructionCalculatePathMultPointsSamplePos instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();

        public DJKVehiclePoint()
            : base()
        {
            this.Transform = null!;
            this.Name = string.Empty;
        }

        public DJKVehiclePoint(string name)
            : base()
        {
            this.Transform = null!;
            this.Name = name;
        }

        public override IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            instruction = InternManager.Instance.Pools.Get<InstructionCalculatePathMultPointsSamplePos>();
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

        private IEnumerable<Vector3> GetWorldPoints()
        {
            pointsResults.Clear();
            for (int i = 0; i < points.Length; i++)
            {
                pointsResults.Add(Transform.position + Transform.rotation * points[i]);
            }
            return pointsResults;
        }

        public override IEnumerable<Vector3> GetAllPoints()
        {
            return GetWorldPoints();
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            Vector3 bestPoint = default;
            float bestSqrDist = float.MaxValue;
            bool found = false;

            foreach (var p in GetWorldPoints())
            {
                if (Mathf.Abs(p.y - point.y) > Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                    continue;

                float sqrDist = (p - point).sqrMagnitude;

                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    bestPoint = p;
                    found = true;
                }
            }

            return found ? bestPoint : Transform.position;
        }

        public override IEnumerable<Vector3> GetNearbyPoints(Vector3 point)
        {
            // pointsResults
            GetWorldPoints();

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

        public override string ToString()
        {
            _pathSb.Clear();

            _pathSb.Append("DJKVehiclePoint \"");
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

        public void CopyFrom(DJKVehiclePoint other)
        {
            Id = other.Id;
            Name = other.Name;
            Transform = other.Transform;
        }

        public override IDJKPoint Clone(IPoolManager pool)
        {
            var clone = pool.Get<DJKVehiclePoint>();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
