using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Parameters
{
    public readonly struct InstructionParameters
    {
        public readonly int groupId;

        public readonly Vector3 start;
        public readonly Vector3 target;

        public readonly IDJKPoint startDJKPoint;
        public readonly IDJKPoint targetDJKPoint;

        public readonly NeighborResult resultCallback;

        public InstructionParameters(int groupId, Vector3 start, Vector3 target, IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint,
                                     NeighborResult resultCallback)
        {
            this.groupId = groupId;
            this.start = start;
            this.target = target;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
            this.resultCallback = resultCallback;
        }
    }
}
