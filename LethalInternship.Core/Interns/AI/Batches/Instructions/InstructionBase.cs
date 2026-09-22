using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public abstract class InstructionBase : IInstruction
    {
        public int IdBatch { get; private set; }
        public int GroupId { get; private set; }

        public Vector3 start;
        public Vector3 target;

        public IDJKPoint startDJKPoint = null!;
        public IDJKPoint targetDJKPoint = null!;

        public float samplePosDist;

        protected NeighborResult onNeighborResult = null!;
        protected int fromId;
        protected int toId;
        protected NavMeshPath navPath = new NavMeshPath();

        public abstract void Execute();

        public void Initialize(int idBatch, int groupId,
                               Vector3 start, Vector3 target,
                               IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint,
                               float samplePosDist,
                               int fromId,
                               int toId,
                               NeighborResult resultCallback)
        {
            IdBatch = idBatch;
            GroupId = groupId;
            this.start = start;
            this.target = target;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
            this.samplePosDist = samplePosDist;
            this.fromId = fromId;
            this.toId = toId;
            onNeighborResult = resultCallback;
        }

        public virtual void Reset()
        {
            IdBatch = -2;
            GroupId = -2;

            start = default;
            target = default;

            startDJKPoint = null!;
            targetDJKPoint = null!;

            samplePosDist = 0f;

            onNeighborResult = null!;

            fromId = 0;
            toId = 0;

            navPath.ClearCorners();
        }

        public abstract void ReleaseInPool();
    }
}
