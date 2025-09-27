using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Parameters
{
    public struct InstructionParameters
    {
        public int groupId;

        public Vector3 start;
        public Vector3 target;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public InstructionParameters(int groupId, Vector3 start, Vector3 target, IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint)
        {
            this.groupId = groupId;
            this.start = start;
            this.target = target;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
        }
    }
}
