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
    public class DJKItemPoint : DJKPointBase
    {
        private readonly StringBuilder _pathSb = new StringBuilder(256);

        public string Name { get; private set; }
        public Transform Transform { get; set; }
        public float GrabDistance { get; set; }

        private int checkPoints = 8;
        private InstructionCalculatePathNoPartialsSamplePos instruction = null!;
        private List<Vector3> pointsResults = new List<Vector3>();
        private float angle;

        public DJKItemPoint() : base()
        {
            this.Transform = null!;
            this.Name = string.Empty;
            this.GrabDistance = 0f;
        }

        public void SetName(GrabbableObject grabbableObject)
        {
            Name = grabbableObject.itemProperties.itemName;
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

        private IEnumerable<Vector3> GetWorldPoints()
        {
            pointsResults.Clear();
            for (int i = 0; i < checkPoints; i++)
            {
                angle = (360f / checkPoints) * i;
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                pointsResults.Add(Transform.position + dir * GrabDistance);
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

            Vector3 origin = Transform.position;

            float step = 360f / checkPoints;

            for (int i = 0; i < checkPoints; i++)
            {
                float angle = step * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                Vector3 p = origin + dir * GrabDistance;

                // filtre
                if (p.y - point.y > Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                    continue;

                float sqrDist = (p - point).sqrMagnitude;

                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    bestPoint = p;
                    found = true;
                }
            }

            return found ? bestPoint : origin;
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

            _pathSb.Append("DJKItemPoint \"");
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

        public void CopyFrom(DJKItemPoint other)
        {
            Id = other.Id;
            Name = other.Name;
            Transform = other.Transform;
            GrabDistance = other.GrabDistance;
        }

        public override IDJKPoint Clone(IPoolManager pool)
        {
            var clone = pool.Get<DJKItemPoint>();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
