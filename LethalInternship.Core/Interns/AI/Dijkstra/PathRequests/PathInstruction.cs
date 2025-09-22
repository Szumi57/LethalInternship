using System;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.PathRequests
{
    public struct PathInstruction
    {
        public Vector3 start;
        public Vector3 target;
        public Action<PathResponse> callback;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public PathInstruction(Vector3 start, Vector3 target,
                               Action<PathResponse> callback,
                               IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint)
        {
            this.start = start;
            this.target = target;
            this.callback = callback;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
        }
    }
}
